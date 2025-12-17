using Microsoft.Extensions.Logging;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
using System.Text.Json;

namespace SmartPathBackend.Services
{
    public class PostAiReviewer : IPostAiReviewer
    {
        private readonly ILLMService _llm;
        private readonly ILogger<PostAiReviewer> _logger;

        public PostAiReviewer(ILLMService llm, ILogger<PostAiReviewer> logger)
        {
            _llm = llm;
            _logger = logger;
        }

        public async Task<PostAiReviewResult> ReviewAsync(
            Post post,
            IEnumerable<Category> categories,
            CancellationToken ct)
        {
            // Build category information
            var categoryList = categories.Select(c => new { c.Id, c.Name }).ToList();
            var categoryJson = JsonSerializer.Serialize(categoryList);

            // Get selected category names from the passed categories list (not from post.CategoryPosts navigation property)
            var selectedCategoryIds = post.CategoryPosts?.Select(cp => cp.CategoryId).ToList() ?? new List<Guid>();
            var selectedCategoryNames = categories
                .Where(c => selectedCategoryIds.Contains(c.Id))
                .Select(c => c.Name)
                .ToList();

            // Build content summary
            var contentPreview = post.Content.Length > 1000
                ? post.Content.Substring(0, 1000) + "..."
                : post.Content;

            var systemPrompt = """
You are an AI content moderator for a Q&A and educational platform. Your task is to review posts for:
1. Content appropriateness (no spam, hate speech, inappropriate content)
2. Category relevance (does the post match the selected categories?)
3. Overall quality (is the post helpful and well-formatted?)

You must respond with a single JSON object in this exact format:
{
  "isAppropriate": true|false,
  "categoryMatch": true|false,
  "confidence": 0.0-1.0,
  "reason": "Brief explanation of your decision"
}

Be objective and fair in your assessment. If in doubt, lean towards accepting.
""";

            var userPrompt = $"""
Post Title: {post.Title}
Is Question: {post.IsQuestion}

Content Preview:
{contentPreview}

Available Categories:
{categoryJson}

Selected Categories: {string.Join(", ", selectedCategoryNames)}

Please review this post for appropriateness and category matching.
""";

            string raw = "";
            try
            {
                raw = await _llm.CompleteAsync(systemPrompt, new[] { ("user", userPrompt) }, null, ct);

                _logger.LogInformation("LLM response for post {PostId}: {Response}", post.Id, raw);

                // Clean up the response - remove any surrounding quotes or formatting
                if (string.IsNullOrWhiteSpace(raw))
                {
                    _logger.LogWarning("LLM returned empty response for post {PostId}", post.Id);
                    throw new JsonException("Empty response from LLM");
                }

                // Parse the cleaned AI response
                raw = ExtractJsonPayload(raw);
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                return new PostAiReviewResult(
                    categoryMatch: root.GetProperty("categoryMatch").GetBoolean(),
                    isAppropriate: root.GetProperty("isAppropriate").GetBoolean(),
                    confidence: root.GetProperty("confidence").GetDouble(),
                    reason: root.GetProperty("reason").GetString() ?? "",
                    suggestedCategoryId: null // Could be implemented to suggest different categories
                );
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response for post {PostId}. Response was: '{RawResponse}'", post.Id, raw);

                // Return a default cautious result
                return new PostAiReviewResult(
                    categoryMatch: true,
                    isAppropriate: true,
                    confidence: 0.7,
                    reason: "AI response parsing failed - auto-approved pending review",
                    suggestedCategoryId: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI review failed for post {PostId}", post.Id);

                // Return a default result on any error
                return new PostAiReviewResult(
                    categoryMatch: true,
                    isAppropriate: true,
                    confidence: 0.5,
                    reason: "AI processing error - pending manual review",
                    suggestedCategoryId: null
                );
            }
        }

        private static string ExtractJsonPayload(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new JsonException("Empty response from LLM");

            raw = raw.Trim();

            // Nếu LLM trả về dạng JSON string literal (có quote ngoài và escape \n, \")
            // thì deserialize để unescape cho đúng.
            if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            {
                try
                {
                    raw = JsonSerializer.Deserialize<string>(raw) ?? raw;
                    raw = raw.Trim();
                }
                catch
                {
                    // fallback: bỏ quote ngoài
                    raw = raw.Substring(1, raw.Length - 2).Trim();
                }
            }

            // Remove code fence kiểu ```json ... ```
            if (raw.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = raw.IndexOf('\n');
                if (firstNewline >= 0) raw = raw[(firstNewline + 1)..];
                var lastFence = raw.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0) raw = raw[..lastFence];
                raw = raw.Trim();
            }

            // Trích JSON object/array thật sự
            var objStart = raw.IndexOf('{');
            var arrStart = raw.IndexOf('[');

            int start = (objStart, arrStart) switch
            {
                (-1, -1) => -1,
                (-1, >= 0) => arrStart,
                ( >= 0, -1) => objStart,
                _ => Math.Min(objStart, arrStart)
            };

            if (start < 0)
                throw new JsonException($"No JSON object/array found in LLM response. Head: {raw[..Math.Min(raw.Length, 40)]}");

            raw = raw[start..];

            int endObj = raw.LastIndexOf('}');
            int endArr = raw.LastIndexOf(']');
            int end = Math.Max(endObj, endArr);

            if (end < 0)
                throw new JsonException("JSON start found but no valid closing brace/bracket found.");

            return raw[..(end + 1)].Trim();
        }
    }
}