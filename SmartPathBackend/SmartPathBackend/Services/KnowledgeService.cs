using Pgvector;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Utils;

namespace SmartPathBackend.Services
{
    public class KnowledgeService: IKnowledgeService
    {
        private readonly IEmbedderService _embedder;
        private readonly IUnitOfWork _uow;

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

        public async Task<Guid> IngestPdfUrlAsync(string url, string? title = null, CancellationToken ct = default)
        {
            using var http = new HttpClient();
            using var res = await http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();
            var bytes = await res.Content.ReadAsByteArrayAsync(ct);

            var text = PdfText.ExtractText(bytes);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("PDF không có text hoặc không trích xuất được.");

            title ??= System.IO.Path.GetFileName(url);
            return await IngestRawAsync(title, url, text, ct);
        }
    }
}
