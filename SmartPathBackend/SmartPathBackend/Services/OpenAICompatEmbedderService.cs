using SmartPathBackend.Interfaces.Services;

namespace SmartPathBackend.Services
{
    public class OpenAICompatEmbedderService: IEmbedderService
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public OpenAICompatEmbedderService(HttpClient http, string model)
        {
            _http = http; _model = model;
        }

        public async Task<float[]> EmbedOneAsync(string input, CancellationToken ct = default)
            => (await EmbedManyAsync(new[] { input }, ct))[0];

        public async Task<List<float[]>> EmbedManyAsync(IEnumerable<string> inputs, CancellationToken ct = default)
        {
            var payload = new { model = _model, input = inputs.ToArray() };
            using var res = await _http.PostAsJsonAsync("/v1/embeddings", payload, ct);
            res.EnsureSuccessStatusCode();
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.GetProperty("data").EnumerateArray()
                     .Select(e => e.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray())
                     .ToList();
        }
    }
}
