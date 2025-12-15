using SmartPathBackend.Interfaces.Services;
using System.Text.Json;

namespace SmartPathBackend.Services
{
    public record AiReviewResult(bool categoryMatch, double confidence, string reason, string? suggestedCategoryPath);

    public interface IStudyMaterialAiReviewer
    {
        Task<AiReviewResult> ReviewAsync(string categoryPath, string title, string? description, string? extractedText, IEnumerable<string> candidateCategoryPaths, CancellationToken ct);
    }

    public class StudyMaterialAiReviewer : IStudyMaterialAiReviewer
    {
        private readonly ILLMService _llm;

        public StudyMaterialAiReviewer(ILLMService llm) => _llm = llm;

        public async Task<AiReviewResult> ReviewAsync(
            string categoryPath, string title, string? description, string? extractedText,
            IEnumerable<string> candidateCategoryPaths, CancellationToken ct)
        {
            var system = """
Bạn là hệ thống kiểm duyệt học liệu. Nhiệm vụ:
- Kiểm tra tài liệu có thuộc đúng categoryPath không.
- Nếu không đúng, gợi ý categoryPath phù hợp nhất trong candidateCategoryPaths.
Trả về JSON duy nhất theo schema:
{"categoryMatch":true|false,"confidence":0..1,"reason":"...","suggestedCategoryPath":null|"..."}
Không thêm chữ ngoài JSON.
""";

            var prompt = $"""
categoryPath: {categoryPath}
title: {title}
description: {description ?? ""}
extractedText (truncated): {(extractedText ?? "").Substring(0, Math.Min(2000, (extractedText ?? "").Length))}
candidateCategoryPaths: {string.Join(" | ", candidateCategoryPaths.Take(50))}
""";

            var raw = await _llm.CompleteAsync(system, new[] { ("user", prompt) }, null, ct);

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            return new AiReviewResult(
                root.GetProperty("categoryMatch").GetBoolean(),
                root.GetProperty("confidence").GetDouble(),
                root.GetProperty("reason").GetString() ?? "",
                root.TryGetProperty("suggestedCategoryPath", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString() : null
            );
        }
    }
}
