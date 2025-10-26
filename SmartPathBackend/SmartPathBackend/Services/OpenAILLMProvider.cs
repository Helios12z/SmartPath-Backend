using Microsoft.Extensions.Options;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Utils;
using System.Text.Json;

namespace SmartPathBackend.Services
{
    public class OpenAILLMProvider : ILLMProvider
    {
        public string Name => "OpenAI";

        private readonly HttpClient _http;
        private readonly LLMOptions _opt;

        public OpenAILLMProvider(IHttpClientFactory factory, IOptions<LLMOptions> opt)
        {
            _opt = opt.Value;
            _http = factory.CreateClient("OpenAI");
        }

        public async Task<string> CompleteAsync(
            string? systemPrompt,
            IEnumerable<(string role, string content)> messages,
            string? modelOverride = null,
            CancellationToken ct = default)
        {
            var model = modelOverride ?? _opt.Model ?? "gpt-4o-mini";

            var openaiMessages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                openaiMessages.Add(new { role = "system", content = systemPrompt });

            openaiMessages.AddRange(messages.Select(m => new { role = m.role, content = m.content }));

            var payload = new { model, messages = openaiMessages };

            using var res = await _http.PostAsJsonAsync("/chat/completions", payload, ct);
            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }
    }
}
