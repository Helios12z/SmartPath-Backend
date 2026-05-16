using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IIntelligentFileSummarizer
    {
        Task<FileSummaryResult> SummarizeFileAsync(
            string title,
            string? description,
            string fileName,
            long fileSize,
            string contentType,
            Stream fileStream,
            CancellationToken ct = default
        );

        Task<FileSummaryResult> SummarizeTextAsync(
            string title,
            string? description,
            string rawText,
            CancellationToken ct = default
        );

        Task<string> GenerateSummarizationPromptAsync(
            string categoryPath,
            FileSummaryResult summary,
            CancellationToken ct = default
        );
    }
}