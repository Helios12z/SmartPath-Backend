using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using Pgvector;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IKnowledgeRepository
    {
        Task<Guid> AddDocumentAsync(KnowledgeDocument doc, CancellationToken ct = default);
        Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct = default);
        Task<List<KnowledgeChunk>> SearchByEmbeddingAsync(float[] queryVec, int topK, CancellationToken ct = default);
        Task<IReadOnlyList<KnowledgeSearchHit>> SearchByVectorAsync(Vector query, int topK, CancellationToken ct = default);
    }
}
