using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IKnowledgeSearchService
    {
        Task<IReadOnlyList<KnowledgeSearchHit>> SearchAsync(string query, int k = 5, CancellationToken ct = default);
    }
}
