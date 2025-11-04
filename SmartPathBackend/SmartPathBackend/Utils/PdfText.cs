namespace SmartPathBackend.Utils
{
    public static class PdfText
    {
        public static string ExtractText(ReadOnlySpan<byte> bytes, int maxChars = 200_000)
        {
            using var ms = new MemoryStream(bytes.ToArray());
            using var doc = UglyToad.PdfPig.PdfDocument.Open(ms);
            var sb = new System.Text.StringBuilder(capacity: Math.Min(maxChars, 256_000));
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (sb.Length + text.Length > maxChars)
                {
                    sb.Append(text.AsSpan(0, Math.Max(0, maxChars - sb.Length)));
                    break;
                }
                sb.AppendLine(text);
            }
            return sb.ToString();
        }
    }
}
