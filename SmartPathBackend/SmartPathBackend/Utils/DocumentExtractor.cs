using DocumentFormat.OpenXml.Packaging;
using HtmlAgilityPack;
using System.IO;
using System.Net.Mime;
using System.Text;
using TikaOnDotNet.TextExtraction;

namespace SmartPathBackend.Utils
{
    public static class DocumentExtractor
    {
        public static async Task<string> ExtractTextAsync(Stream stream, string? contentType, string fileName, CancellationToken ct = default)
        {
            if (stream.CanSeek) stream.Position = 0;
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

            // 1) PDF
            if (Is(contentType, "application/pdf") || ext == ".pdf")
            {
                using var ms = await ToMemoryStream(stream, ct);
                return PdfText.ExtractText(ms.ToArray()) ?? string.Empty;
            }

            // 2) DOCX
            if (Is(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document") || ext == ".docx")
            {
                using var ms = await ToMemoryStream(stream, ct);
                using var wordDoc = WordprocessingDocument.Open(ms, false);
                return wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
            }

            // 3) .doc
            if (ext == ".doc" || Is(contentType, "application/msword"))
            {
                using var ms = await ToMemoryStream(stream, ct);
                return new TextExtractor().Extract(ms.ToArray())?.Text ?? string.Empty;
            }

            // 4) HTML
            if (Is(contentType, "text/html") || ext is ".html" or ".htm")
            {
                using var r = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
                var html = await r.ReadToEndAsync(ct);
                return HtmlToPlainText(html);
            }

            // 5) Fallback
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            return await reader.ReadToEndAsync(ct);
        }

        public static string HtmlToPlainText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script/style/nav/footer/header
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//header|//footer|//nav|//aside");
            if (nodesToRemove != null)
            {
                foreach (var n in nodesToRemove) n.Remove();
            }

            var text = doc.DocumentNode.InnerText;
            return HtmlEntity.DeEntitize(text)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
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
    }
}
