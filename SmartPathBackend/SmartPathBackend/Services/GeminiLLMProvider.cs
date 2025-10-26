using Microsoft.Extensions.Options;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Utils;
using System.Text.Json;

namespace SmartPathBackend.Services
{
    public class GeminiLLMProvider : ILLMProvider
    {
        public string Name => "Gemini";

        private readonly HttpClient _http;
        private readonly LLMOptions _opt;

        public GeminiLLMProvider(IHttpClientFactory factory, IOptions<LLMOptions> opt)
        {
            _opt = opt.Value;
            _http = factory.CreateClient("Gemini");
        }

        public async Task<string> CompleteAsync(
            string? systemPrompt,
            IEnumerable<(string role, string content)> messages,
            string? modelOverride = null,
            CancellationToken ct = default)
        {
            var model = modelOverride ?? _opt.Model ?? "gemini-1.5-flash";

            var contents = new List<object>();
            foreach (var (role, content) in messages)
            {
                // Map: assistant -> model; user -> user; system -> user (nếu có trong history)
                var r = role switch
                {
                    "assistant" => "model",
                    "system" => "user",
                    _ => "user"
                };
                contents.Add(new
                {
                    role = r,
                    parts = new[] { new { text = content } }
                });
            }

            var body = new Dictionary<string, object?>
            {
                ["contents"] = contents
            };

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                body["systemInstruction"] = new
                {
                    role = "system",
                    parts = new[] { new { text = systemPrompt } }
                };
            }

            // Bạn có thể thêm generationConfig/safetySettings nếu cần:
            // body["generationConfig"] = new { temperature = 0.7 };

            var url = $"/v1beta/models/{model}:generateContent";
            using var res = await _http.PostAsJsonAsync(url, body, ct);
            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            // Đường dẫn tới text:
            // candidates[0].content.parts[0].text
            var root = doc.RootElement;
            var text = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }
    }
}
