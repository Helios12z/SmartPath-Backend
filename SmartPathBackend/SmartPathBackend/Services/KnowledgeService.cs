using DocumentFormat.OpenXml.Packaging;
using HtmlAgilityPack;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Utils;
using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using TikaOnDotNet.TextExtraction;

namespace SmartPathBackend.Services
{
    public class KnowledgeService: IKnowledgeService
    {
        private readonly IEmbedderService _embedder;
        private readonly IUnitOfWork _uow;
        private readonly FileExtensionContentTypeProvider _contentTypes = new();
        private readonly ILogger<KnowledgeService> _logger;
        private readonly IWebCrawlerService _crawler;

        public KnowledgeService(IEmbedderService embedder, IKnowledgeRepository repo, IUnitOfWork uow, ILogger<KnowledgeService> logger, IWebCrawlerService crawler)
        {
            _embedder = embedder;
            _uow = uow;
            _logger = logger;
            _crawler = crawler;
        }

        public async Task<Guid> IngestRawAsync(string title, string? sourceUrl, string rawText, CancellationToken ct = default)
        {
            // 1) Tạo document
            var doc = new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = title?.Trim(),
                SourceUrl = sourceUrl,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Knowledges.AddDocumentAsync(doc, ct);

            var chunks = ChunkByTokens(rawText, targetTokens: 350, overlap: 40);
            if (chunks.Count == 0) return doc.Id;

            var embeds = await _embedder.EmbedManyAsync(chunks.Select(x => x.Text), ct);
            if (embeds.Count != chunks.Count)
                throw new InvalidOperationException($"Embedding count mismatch: chunks={chunks.Count}, embeds={embeds.Count}");

            var rows = chunks.Select((c, i) => new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                ChunkIndex = i,
                Content = c.Text,
                Embedding = new Vector(embeds[i]),   
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _uow.Knowledges.AddChunksAsync(rows, ct);
            await _uow.SaveChangesAsync();

            return doc.Id;
        }

        private sealed record Piece(string Text);

        private static List<Piece> ChunkByTokens(string text, int targetTokens, int overlap)
        {
            var parts = new List<Piece>();
            var sentences = text.Split(new[] { "\n\n", "\n", ". " }, StringSplitOptions.RemoveEmptyEntries);
            var buff = new List<string>();
            int count = 0;
            foreach (var s in sentences)
            {
                buff.Add(s);
                count += s.Length / 4; 
                if (count >= targetTokens)
                {
                    parts.Add(new Piece(string.Join(" ", buff)));
                    // overlap
                    buff = buff.Skip(Math.Max(0, buff.Count - 1)).ToList();
                    count = buff.Sum(x => x.Length / 4);
                }
            }
            if (buff.Count > 0) parts.Add(new Piece(string.Join(" ", buff)));
            return parts;
        }

        public async Task<Guid> IngestFromUrlAsync(string url, string? title = null, CancellationToken ct = default)
        {
            // depth = 2 as requested
            var pages = await _crawler.CrawlAsync(url, maxDepth: 2, maxPages: 20, ct: ct);
            Guid firstDocId = Guid.Empty;

            foreach (var page in pages)
            {
                // Tránh ingest trùng document theo SourceUrl
                var existed = await _uow.Knowledges
                    .QueryDocuments()
                    .AnyAsync(d => d.SourceUrl == page.Url, ct);

                if (existed) continue;

                var id = await IngestRawAsync(page.Title, page.Url, page.Content, ct);
                if (firstDocId == Guid.Empty) firstDocId = id;
            }

            return firstDocId;
        }

        public async Task<Guid> IngestFileAsync(
            string title,
            string? sourceUrl,
            Stream fileStream,
            string? contentType,
            string fileName,
            CancellationToken ct = default)
        {
            var text = await ExtractTextAsync(fileStream, contentType, fileName, ct);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Không trích xuất được văn bản từ file.");

            return await IngestRawAsync(title, sourceUrl, text, ct);
        }

        private static string GuessFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var name = System.IO.Path.GetFileName(uri.LocalPath);
                return string.IsNullOrWhiteSpace(name) ? "download" : name;
            }
            catch { return "download"; }
        }

        private static async Task<string> ExtractTextAsync(Stream stream, string? contentType, string fileName, CancellationToken ct)
        {
            return await DocumentExtractor.ExtractTextAsync(stream, contentType, fileName, ct);
        }

        private static bool Is(string? contentType, string target)
            => string.Equals(contentType, target, StringComparison.OrdinalIgnoreCase);

        private static string NaiveStripRtf(string rtf)
        {
            if (string.IsNullOrEmpty(rtf)) return string.Empty;
            var s = System.Text.RegularExpressions.Regex.Replace(rtf, @"\\[a-z]+\d* ?", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[{}]", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ").Trim();
            return s;
        }

        public async Task<List<KnowledgeDocumentDto>> GetDocumentsAsync(string? q, CancellationToken ct = default)
        {
            var query = _uow.Knowledges.QueryDocuments();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(d =>
                    (d.Title != null && EF.Functions.ILike(d.Title, $"%{term}%")) ||
                    (d.SourceUrl != null && EF.Functions.ILike(d.SourceUrl, $"%{term}%")));
            }

            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new KnowledgeDocumentDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    SourceUrl = d.SourceUrl,
                    Meta = d.Meta,
                    CreatedAt = d.CreatedAt,
                    ChunkCount = d.Chunks.Count
                })
                .ToListAsync(ct);

            return items;
        }

        public async Task<KnowledgeDocumentDto?> GetDocumentAsync(Guid id, CancellationToken ct = default)
        {
            var doc = await _uow.Knowledges.FindDocumentAsync(id, ct);
            if (doc == null) return null;

            return new KnowledgeDocumentDto
            {
                Id = doc.Id,
                Title = doc.Title,
                SourceUrl = doc.SourceUrl,
                Meta = doc.Meta,
                CreatedAt = doc.CreatedAt,
                ChunkCount = doc.Chunks.Count
            };
        }

        private async Task<List<KnowledgeDocumentDto>> FindRelatedDocumentsAsync(
            string? title,
            string? sourceUrl,
            CancellationToken ct)
        {
            // Nếu không có gì thì trả rỗng
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(sourceUrl))
                return new List<KnowledgeDocumentDto>();

            var baseQuery = _uow.Knowledges.QueryDocuments();

            // Lọc sơ bộ theo host (nếu có SourceUrl) để đỡ quét cả bảng
            string? host = null;
            if (!string.IsNullOrWhiteSpace(sourceUrl))
            {
                try
                {
                    var uri = new Uri(sourceUrl);
                    host = uri.Host;
                }
                catch { /* ignore */ }
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                baseQuery = baseQuery.Where(d => d.SourceUrl != null && d.SourceUrl.Contains(host));
            }

            // Lấy candidates có Title (limit cứng, tuỳ bạn scale)
            var candidates = await baseQuery
                .Where(d => d.Title != null)
                .Take(500) // nếu doc nhiều có thể paging, hoặc tăng lên nếu ít
                .ToListAsync(ct);

            // Chuẩn hoá title mới
            var rawTitle = title;
            if (string.IsNullOrWhiteSpace(rawTitle) && !string.IsNullOrWhiteSpace(sourceUrl))
            {
                try
                {
                    var uri = new Uri(sourceUrl);
                    rawTitle = Path.GetFileNameWithoutExtension(uri.LocalPath);
                }
                catch
                {
                    rawTitle = sourceUrl;
                }
            }

            if (string.IsNullOrWhiteSpace(rawTitle))
                return new List<KnowledgeDocumentDto>();

            var normalizedNew = NormalizeTitleForMatch(rawTitle);

            // Tính similarity với từng doc, lọc theo ngưỡng
            const double SIM_THRESHOLD = 0.5; // tuỳ chỉnh
            var related = candidates
                .Select(d => new
                {
                    Doc = d,
                    Score = ComputeTitleSimilarity(normalizedNew, NormalizeTitleForMatch(d.Title!))
                })
                .Where(x => x.Score >= SIM_THRESHOLD)
                .OrderByDescending(x => x.Score)
                .Take(20)
                .Select(x => new KnowledgeDocumentDto
                {
                    Id = x.Doc.Id,
                    Title = x.Doc.Title,
                    SourceUrl = x.Doc.SourceUrl,
                    Meta = x.Doc.Meta,
                    CreatedAt = x.Doc.CreatedAt,
                    ChunkCount = x.Doc.Chunks.Count
                })
                .ToList();

            return related;
        }

        public async Task<KnowledgePreviewResultDTO> PreviewByMetadataAsync(
            string? title,
            string? sourceUrl,
            CancellationToken ct = default)
        {
            var related = await FindRelatedDocumentsAsync(title, sourceUrl, ct);

            return new KnowledgePreviewResultDTO
            {
                ProposedTitle = string.IsNullOrWhiteSpace(title) ? "(no title)" : title.Trim(),
                ProposedSourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl,
                RelatedDocuments = related
            };
        }

        public async Task<bool> UpdateDocumentAsync(Guid id, KnowledgeDocumentUpdateRequest req, CancellationToken ct = default)
        {
            var doc = await _uow.Knowledges.FindDocumentAsync(id, ct);
            if (doc == null) return false;

            if (req.Title != null) doc.Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim();
            if (req.Meta != null) doc.Meta = string.IsNullOrWhiteSpace(req.Meta) ? null : req.Meta.Trim();

            await _uow.Knowledges.SaveAsync(ct);
            return true;
        }

        public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default)
        {
            var doc = await _uow.Knowledges.FindDocumentAsync(id, ct);
            if (doc == null) return false;

            // already have cascade delete on FK
            // await _uow.Knowledges.RemoveChunksByDocumentAsync(id, ct);

            await _uow.Knowledges.RemoveDocumentAsync(doc, ct);
            return true;
        }

        private static string NormalizeTitleForMatch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var s = input.ToLowerInvariant();

            // Bỏ dấu tiếng Việt (nếu dùng tiếng Việt có dấu)
            s = RemoveDiacritics(s);

            // Thay _ - . thành space
            s = s.Replace("_", " ")
                 .Replace("-", " ")
                 .Replace(".", " ");

            // Bỏ mọi thứ không phải chữ/số
            s = Regex.Replace(s, @"[^\p{L}\p{Nd}]+", " ");

            // Gom space
            s = Regex.Replace(s, @"\s+", " ").Trim();

            if (string.IsNullOrEmpty(s))
                return string.Empty;

            // Tách tokens
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Map synonym / viết tắt
            for (int i = 0; i < tokens.Count; i++)
            {
                tokens[i] = MapTokenSynonym(tokens[i]);
            }

            // Ghép lại thành chuỗi normalize cuối
            return string.Join(" ", tokens);
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Map các dạng tương đương về 1 token chung
        private static string MapTokenSynonym(string token)
        {
            // lump một số trường hợp phổ biến, dễ mở rộng
            switch (token)
            {
                case "sinh":
                case "sinhvien":
                case "sv":
                    return "sv";

                case "vien":
                    return "vien"; // tuỳ, có thể bỏ nếu không cần

                case "sv5t":
                case "sinhvien5tot":
                case "5tot":
                    return "sv5t";

                case "hocki":
                case "hoc":
                case "ki":
                case "ky":
                case "hk":
                    return "hk";

                case "quy":
                case "quydinh":
                case "quy_dinh":
                    return "quydinh";

                default:
                    return token;
            }
        }

        private static double ComputeTitleSimilarity(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return 0.0;

            if (a == b) return 1.0;

            var gramsA = GetBigrams(a);
            var gramsB = GetBigrams(b);

            if (gramsA.Count == 0 || gramsB.Count == 0)
                return 0.0;

            int intersect = gramsA.Intersect(gramsB).Count();
            return (2.0 * intersect) / (gramsA.Count + gramsB.Count);
        }

        private static HashSet<string> GetBigrams(string s)
        {
            var set = new HashSet<string>();
            var clean = s.Replace(" ", ""); // bỏ space để lấy bigram liên tục

            if (clean.Length <= 1)
            {
                if (!string.IsNullOrEmpty(clean))
                    set.Add(clean);
                return set;
            }

            for (int i = 0; i < clean.Length - 1; i++)
            {
                set.Add(clean.Substring(i, 2));
            }
            return set;
        }
    }
}
