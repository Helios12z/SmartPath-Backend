using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using Pgvector;

namespace SmartPathBackend.Services
{
    public sealed class KnowledgeSearchService : IKnowledgeSearchService
    {
        private readonly IEmbedderService _embedder;
        private readonly IUnitOfWork _uow;

        public KnowledgeSearchService(IEmbedderService embedder, IUnitOfWork uow)
        {
            _embedder = embedder;
            _uow = uow;
        }

        public async Task<IReadOnlyList<KnowledgeSearchHit>> SearchAsync(string query, int k = 5, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return Array.Empty<KnowledgeSearchHit>();

            var emb = await _embedder.EmbedOneAsync(query, ct);
            var vec = new Vector(emb);

            var hits = await _uow.Knowledges.SearchByVectorAsync(vec, Math.Max(1, k), ct);
            return hits;
        }
    }
}
