using Pgvector;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Utils;
using System.Net.Mime;
using DocumentFormat.OpenXml.Packaging;
using HtmlAgilityPack;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

namespace SmartPathBackend.Services
{
    public class KnowledgeService: IKnowledgeService
    {
        private readonly IEmbedderService _embedder;
        private readonly IUnitOfWork _uow;
        private readonly FileExtensionContentTypeProvider _contentTypes = new();

        public KnowledgeService(IEmbedderService embedder, IKnowledgeRepository repo, IUnitOfWork uow)
        {
            _embedder = embedder;
            _uow = uow; 
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

            string text = await ExtractTextAsync(new MemoryStream(bytes), contentType, fileName, ct);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Không trích xuất được văn bản từ nội dung URL.");

            return await IngestRawAsync(title, url, text, ct);
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

            // 3) Legacy .doc (nếu cần, gợi ý dùng TikaOnDotNet hoặc Aspose; ở đây coi như unsupported)
            if (ext == ".doc" || Is(contentType, "application/msword"))
            {
                // TODO: tích hợp thêm thư viện xử lý .doc nếu cần
                return string.Empty;
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

        private static string NaiveStripRtf(string rtf)
        {
            if (string.IsNullOrEmpty(rtf)) return string.Empty;
            var s = System.Text.RegularExpressions.Regex.Replace(rtf, @"\\[a-z]+\d* ?", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[{}]", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ").Trim();
            return s;
        }
    }
}
