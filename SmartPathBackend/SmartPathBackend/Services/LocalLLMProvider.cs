using SmartPathBackend.Interfaces.Services;
using System.Text;
using System.Text.Json;

namespace SmartPathBackend.Services
{
    public sealed class LocalLLMProvider : ILLMProvider
    {
        public string Name => "LocalLLM";

        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public LocalLLMProvider(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("LocalLLM"); // BaseAddress should be http://127.0.0.1:8000/v1
        }

        public async Task<string> CompleteAsync(
            string? systemPrompt,
            IEnumerable<(string role, string content)> messages,
            string? modelOverride = null,
            CancellationToken ct = default)
        {
            // Build OpenAI-style chat payload
            var openAiMsgs = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                openAiMsgs.Add(new { role = "system", content = systemPrompt });

            openAiMsgs.AddRange(messages.Select(m => new { role = m.role, content = m.content }));

            var payload = new
            {
                model = modelOverride ?? "smartpath-3b",
                messages = openAiMsgs
            };

            // Try modern /v1/chat/completions first
            using var chatReq = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOpts), Encoding.UTF8, "application/json")
            };

            using var chatRes = await _http.SendAsync(chatReq, ct);
            if (chatRes.IsSuccessStatusCode)
            {
                using var chatStream = await chatRes.Content.ReadAsStreamAsync(ct);
                using var chatDoc = await JsonDocument.ParseAsync(chatStream, cancellationToken: ct);
                var content = chatDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return content ?? string.Empty;
            }

            // Fallback to legacy /v1/completions (some llama.cpp builds expose this only)
            var legacyPrompt = BuildLegacyPrompt(systemPrompt, messages);
            var legacyPayload = new
            {
                model = modelOverride ?? "smartpath-3b",
                prompt = legacyPrompt
            };

            using var legacyReq = new HttpRequestMessage(HttpMethod.Post, "/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(legacyPayload, _jsonOpts), Encoding.UTF8, "application/json")
            };

            using var legacyRes = await _http.SendAsync(legacyReq, ct);
            legacyRes.EnsureSuccessStatusCode();

            using var legacyStream = await legacyRes.Content.ReadAsStreamAsync(ct);
            using var legacyDoc = await JsonDocument.ParseAsync(legacyStream, cancellationToken: ct);
            var text = legacyDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }

        private static string BuildLegacyPrompt(string? systemPrompt, IEnumerable<(string role, string content)> messages)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                sb.AppendLine($"SYSTEM: {systemPrompt.Trim()}");
            }
            foreach (var (role, content) in messages)
            {
                if (!string.IsNullOrWhiteSpace(content))
                    sb.AppendLine($"{role.ToUpperInvariant()}: {content.Trim()}");
            }
            sb.Append("ASSISTANT: ");
            return sb.ToString();
        }
    }
}
