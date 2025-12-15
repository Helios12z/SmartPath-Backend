using Microsoft.Extensions.Logging;
using SmartPathBackend.Interfaces.Services;
using System.Text.Json;

namespace SmartPathBackend.Services
{
    public record AiReviewResult(bool categoryMatch, double confidence, string reason, string? suggestedCategoryPath);

    public interface IStudyMaterialAiReviewer
    {
        Task<AiReviewResult> ReviewAsync(string optimizedPrompt, IEnumerable<string> candidateCategoryPaths, CancellationToken ct);
    }

    public class StudyMaterialAiReviewer : IStudyMaterialAiReviewer
    {
        private readonly ILLMService _llm;
        private readonly ILogger<StudyMaterialAiReviewer> _logger;

        public StudyMaterialAiReviewer(ILLMService llm, ILogger<StudyMaterialAiReviewer> logger)
        {
            _llm = llm;
            _logger = logger;
        }

        public async Task<AiReviewResult> ReviewAsync(
            string optimizedPrompt,
            IEnumerable<string> candidateCategoryPaths,
            CancellationToken ct)
        {
            var system = """
You are an AI content reviewer for educational materials. Analyze the provided summary and metadata to determine appropriateness and category matching.

Your response must be a single JSON object exactly in this format:
{"categoryMatch":true|false,"isAppropriate":true|false,"confidence":0.0-1.0,"reason":"brief explanation"}

Be concise but thorough in your analysis.
""";

            // The optimized prompt already contains all necessary information
            var raw = await _llm.CompleteAsync(system, new[] { ("user", optimizedPrompt) }, null, ct);

            // Try to parse as JSON, handle errors gracefully
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // Check if it has the old format (no isAppropriate)
                if (root.TryGetProperty("isAppropriate", out _) == false)
                {
                    // Handle old format without isAppropriate field
                    return new AiReviewResult(
                        root.GetProperty("categoryMatch").GetBoolean(),
                        root.GetProperty("confidence").GetDouble(),
                        root.GetProperty("reason").GetString() ?? "",
                        root.TryGetProperty("suggestedCategoryPath", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString() : null
                    );
                }
                else
                {
                    // New format with isAppropriate
                    return new AiReviewResult(
                        root.GetProperty("categoryMatch").GetBoolean(),
                        root.GetProperty("confidence").GetDouble(),
                        root.GetProperty("reason").GetString() ?? "",
                        root.TryGetProperty("suggestedCategoryPath", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString() : null
                    );
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response as JSON. Raw response: {RawResponse}", raw);

                // Return a default cautious result for unparseable responses
                return new AiReviewResult(
                    categoryMatch: false,
                    confidence: 0.3,
                    reason: "Failed to parse AI response - requires manual review",
                    suggestedCategoryPath: null
                );
            }
        }
    }
}
