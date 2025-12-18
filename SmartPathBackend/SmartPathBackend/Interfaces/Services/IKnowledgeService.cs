using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IKnowledgeService
    {
        Task<Guid> IngestRawAsync(string title, string? sourceUrl, string rawText, CancellationToken ct = default);
        Task<Guid> IngestFromUrlAsync(string url, string? title = null, CancellationToken ct = default);
        Task<Guid> IngestFileAsync(string title, string? sourceUrl, Stream fileStream, string? contentType, string fileName, CancellationToken ct = default);
        Task<List<KnowledgeDocumentDto>> GetDocumentsAsync(string? q, CancellationToken ct = default);
        Task<KnowledgeDocumentDto?> GetDocumentAsync(Guid id, CancellationToken ct = default);
        Task<bool> UpdateDocumentAsync(Guid id, KnowledgeDocumentUpdateRequest req, CancellationToken ct = default);
        Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default);
        Task<KnowledgePreviewResultDTO> PreviewByMetadataAsync(string? title, string? sourceUrl, CancellationToken ct = default);
    }
}
