namespace SmartPathBackend.Interfaces.Services
{
    public interface IKnowledgeIngestService
    {
        Task<Guid> IngestRawAsync(string title, string? sourceUrl, string rawText, CancellationToken ct = default);
        Task<Guid> IngestPdfUrlAsync(string url, string? title = null, CancellationToken ct = default);
    }
}
