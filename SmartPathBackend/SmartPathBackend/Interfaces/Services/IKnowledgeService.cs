namespace SmartPathBackend.Interfaces.Services
{
    public interface IKnowledgeService
    {
        Task<Guid> IngestRawAsync(string title, string? sourceUrl, string rawText, CancellationToken ct = default);
        Task<Guid> IngestFromUrlAsync(string url, string? title = null, CancellationToken ct = default); // NEW
        Task<Guid> IngestFileAsync(string title, string? sourceUrl, Stream fileStream, string? contentType, string fileName, CancellationToken ct = default);
    }
}
