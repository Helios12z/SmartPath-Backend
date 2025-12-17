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

        public KnowledgeService(IEmbedderService embedder, IKnowledgeRepository repo, IUnitOfWork uow, ILogger<KnowledgeService> logger)
        {
            _embedder = embedder;
            _uow = uow;
            _logger = logger;
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
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // maxDepth = 1: url gốc + các link con trực tiếp
            return await IngestFromUrlInternalAsync(url, title, visited, depth: 0, maxDepth: 1, ct);
        }

        private async Task<Guid> IngestFromUrlInternalAsync(
            string url,
            string? title,
            HashSet<string> visited,
            int depth,
            int maxDepth,
            CancellationToken ct)
        {
            // Nếu đã ingest URL này rồi thì bỏ qua
            if (!visited.Add(url))
            {
                return Guid.Empty;
            }

            using var http = new HttpClient();

            using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            res.EnsureSuccessStatusCode();

            var contentType = res.Content.Headers.ContentType?.MediaType;
            var fileName = GuessFileNameFromUrl(url);
            if (string.IsNullOrWhiteSpace(contentType) &&
                _contentTypes.TryGetContentType(fileName, out var guessed))
            {
                contentType = guessed;
            }

            var bytes = await res.Content.ReadAsByteArrayAsync(ct);
            title ??= System.IO.Path.GetFileNameWithoutExtension(fileName);
            var ext = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();

            // Nếu là HTML: tự xử lý, vừa ingest text vừa crawl link con
            if (IsHtml(contentType, ext))
            {
                var html = Encoding.UTF8.GetString(bytes);
                var text = HtmlToPlainText(html);
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Không trích xuất được văn bản từ nội dung URL.");

                // 1) Ingest document cho TRANG HIỆN TẠI
                var docId = await IngestRawAsync(title, url, text, ct);

                // 2) Nếu còn depth, crawl các link con
                if (depth < maxDepth)
                {
                    var childLinks = ExtractChildLinks(html, url);

                    foreach (var childUrl in childLinks)
                    {
                        // Tránh ingest trùng document theo SourceUrl
                        var existed = await _uow.Knowledges
                            .QueryDocuments()
                            .AnyAsync(d => d.SourceUrl == childUrl, ct);

                        if (existed) continue;

                        try
                        {
                            await IngestFromUrlInternalAsync(
                                childUrl,
                                title: null,
                                visited: visited,
                                depth: depth + 1,
                                maxDepth: maxDepth,
                                ct: ct
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process URL: {Url} for document: {DocumentId}", url, docId);
                        }
                    }
                }

                return docId;
            }
            else
            {
                // Các loại file khác: dùng ExtractTextAsync như cũ
                string text = await ExtractTextAsync(new MemoryStream(bytes), contentType, fileName, ct);
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Không trích xuất được văn bản từ nội dung URL.");

                return await IngestRawAsync(title, url, text, ct);
            }
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
            // Đảm bảo có thể đọc lại từ đầu
            if (stream.CanSeek) stream.Position = 0;

            // Ưu tiên content-type, fallback theo extension
            var ext = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();

            // 1) PDF
            if (Is(contentType, MediaTypeNames.Application.Pdf) || ext == ".pdf")
            {
                using var ms = await ToMemoryStream(stream, ct);
                var bytes = ms.ToArray();
                return PdfText.ExtractText(bytes) ?? string.Empty;
            }

            // 2) DOCX
            if (Is(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document") || ext == ".docx")
            {
                using var ms = await ToMemoryStream(stream, ct);
                using var wordDoc = WordprocessingDocument.Open(ms, false);
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                return body?.InnerText ?? string.Empty;
            }

            // 3) .doc 
            if (ext == ".doc" || Is(contentType, "application/msword"))
            {
                using var ms = await ToMemoryStream(stream, ct);
                var extractor = new TextExtractor();
                var bytes = ms.ToArray();
                var result = extractor.Extract(bytes);
                return result?.Text ?? string.Empty;
            }

            // 4) HTML
            if (Is(contentType, MediaTypeNames.Text.Html) || ext is ".html" or ".htm")
            {
                using var r = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var html = await r.ReadToEndAsync(ct);
                return HtmlToPlainText(html);
            }

            // 5) Markdown (đọc như text thuần, có thể strip markdown sau)
            if (ext == ".md" || Is(contentType, "text/markdown"))
            {
                using var r = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var md = await r.ReadToEndAsync(ct);
                return StripMarkdown(md);
            }

            // 6) RTF (đơn giản: strip thô; muốn chuẩn hơn, dùng thư viện RtfPipe)
            if (ext == ".rtf" || Is(contentType, "application/rtf") || Is(contentType, "text/rtf"))
            {
                using var r = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var rtf = await r.ReadToEndAsync(ct);
                return NaiveStripRtf(rtf);
            }

            // 7) TXT & các loại text/*
            if ((contentType?.StartsWith("text/") ?? false) || ext is ".txt" or ".csv" or ".log")
            {
                using var r = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                return await r.ReadToEndAsync(ct);
            }

            // 8) Fallback: cố đọc như UTF-8 text
            using (var r2 = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            {
                var raw = await r2.ReadToEndAsync(ct);
                return raw;
            }
        }

        private static bool Is(string? contentType, string target)
            => string.Equals(contentType, target, StringComparison.OrdinalIgnoreCase);

        private static async Task<MemoryStream> ToMemoryStream(Stream s, CancellationToken ct)
        {
            var ms = new MemoryStream();
            if (s.CanSeek) s.Position = 0;
            await s.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }

        private static string HtmlToPlainText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Loại script/style
            foreach (var n in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
                n.Remove();

            var text = doc.DocumentNode.InnerText;
            // Chuẩn hoá khoảng trắng
            return HtmlEntity.DeEntitize(text)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
        }

        private static string StripMarkdown(string md)
        {
            if (string.IsNullOrEmpty(md)) return string.Empty;
            var s = md;

            // code fences
            s = System.Text.RegularExpressions.Regex.Replace(s, "```[\\s\\S]*?```", " ");
            // inline code
            s = System.Text.RegularExpressions.Regex.Replace(s, "`[^`]*`", " ");
            // images/links: [text](url)
            s = System.Text.RegularExpressions.Regex.Replace(s, "!?\\[[^\\]]*\\]\\([^\\)]*\\)", " ");
            // headings/lists/formatting
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[#>*_\-\+\=]{1,}", " ");
            // html tags if any
            s = System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", " ");
            // normalize spaces
            s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ").Trim();

            return s;
        }

        private static bool IsHtml(string? contentType, string? ext)
        {
            return Is(contentType, MediaTypeNames.Text.Html)
                   || ext is ".html" or ".htm";
        }

        private static List<string> ExtractChildLinks(string html, string baseUrl)
        {
            var result = new List<string>();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes == null || nodes.Count == 0)
                return result;

            var baseUri = new Uri(baseUrl);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                var href = node.GetAttributeValue("href", "").Trim();
                if (string.IsNullOrEmpty(href))
                    continue;

                // bỏ qua link anchor / javascript / mailto
                if (href.StartsWith("#") ||
                    href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // build absolute url
                Uri abs;
                if (!Uri.TryCreate(href, UriKind.Absolute, out abs))
                {
                    abs = new Uri(baseUri, href);
                }

                // chỉ crawl cùng domain (tránh bay ra ngoài UIT, v.v.)
                if (!string.Equals(abs.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
                    continue;

                var childUrl = abs.ToString();
                if (!seen.Add(childUrl))
                    continue;

                // lọc loại file cần crawl: pdf, doc, docx, html
                var childExt = System.IO.Path.GetExtension(abs.LocalPath)?.ToLowerInvariant();
                if (childExt is not (".pdf" or ".doc" or ".docx" or ".html" or ".htm"))
                    continue;

                result.Add(childUrl);
            }

            return result;
        }

        private static string NaiveStripRtf(string rtf)
        {
            if (string.IsNullOrEmpty(rtf)) return string.Empty;
            var s = System.Text.RegularExpressions.Regex.Replace(rtf, @"\\[a-z]+\d* ?", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[{}]", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ").Trim();
            return s;
        }

        public async Task<PagedResult<KnowledgeDocumentDto>> GetDocumentsAsync(int page, int pageSize, string? q, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _uow.Knowledges.QueryDocuments();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(d =>
                    (d.Title != null && EF.Functions.ILike(d.Title, $"%{term}%")) ||
                    (d.SourceUrl != null && EF.Functions.ILike(d.SourceUrl, $"%{term}%")));
            }

            var total = await query.CountAsync(ct);

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
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<KnowledgeDocumentDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Items = items
            };
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
