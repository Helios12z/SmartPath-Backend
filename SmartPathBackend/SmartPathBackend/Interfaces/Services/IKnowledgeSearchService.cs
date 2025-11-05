using SmartPathBackend.Models.DTOs;
using Pgvector;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IKnowledgeSearchService
    {
        Task<IReadOnlyList<KnowledgeSearchHit>> SearchByVectorAsync(Vector q, int k, CancellationToken ct = default);
    }
}
