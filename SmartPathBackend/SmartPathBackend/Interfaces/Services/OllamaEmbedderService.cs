using System.Net;
using System.Text;
using System.Text.Json;

namespace SmartPathBackend.Interfaces.Services
{
    public class OllamaEmbedderService: IEmbedderService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _model;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public OllamaEmbedderService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _baseUrl = (cfg["Embedding:BaseUrl"] ?? "http://127.0.0.1:11434").TrimEnd('/');
            _model = cfg["Embedding:Model"] ?? "bge-m3";
        }

        public async Task<float[]> EmbedOneAsync(string input, CancellationToken ct = default)
        {
            var list = await EmbedManyAsync(new[] { input }, ct);
            return list[0];
        }

        public async Task<List<float[]>> EmbedManyAsync(IEnumerable<string> inputs, CancellationToken ct = default)
        {
            var arr = inputs.ToArray();
            if (arr.Length == 0) return new();

            // Ưu tiên OpenAI-compatible /v1/embeddings
            var urlA = $"{_baseUrl}/v1/embeddings";
            var urlB = $"{_baseUrl}/api/embed"; // fallback API gốc của Ollama mới

            var payload = JsonSerializer.Serialize(new { model = _model, input = arr }, JsonOpts);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsync(urlA, content, ct);
            }
            catch
            {
                resp = new(HttpStatusCode.ServiceUnavailable);
            }

            if (!resp.IsSuccessStatusCode)
            {
                using var content2 = new StringContent(payload, Encoding.UTF8, "application/json");
                resp = await _http.PostAsync(urlB, content2, ct);
                
                if (!resp.IsSuccessStatusCode)
                {
                    var errBody = await resp.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException($"Ollama Error ({resp.StatusCode}) for {urlB}: {errBody}");
                }
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("data", out var dataProp))
            {
                // OpenAI-compatible format
                var result = new List<float[]>(capacity: dataProp.GetArrayLength());
                foreach (var item in dataProp.EnumerateArray())
                {
                    var embEl = item.GetProperty("embedding");
                    var vec = new float[embEl.GetArrayLength()];
                    int i = 0;
                    foreach (var num in embEl.EnumerateArray())
                        vec[i++] = (float)num.GetDouble();
                    result.Add(vec);
                }
                return result;
            }
            else if (doc.RootElement.TryGetProperty("embeddings", out var embeddingsProp))
            {
                // Ollama /api/embed format
                var result = new List<float[]>(capacity: embeddingsProp.GetArrayLength());
                foreach (var item in embeddingsProp.EnumerateArray())
                {
                    var vec = new float[item.GetArrayLength()];
                    int i = 0;
                    foreach (var num in item.EnumerateArray())
                        vec[i++] = (float)num.GetDouble();
                    result.Add(vec);
                }
                return result;
            }
            else if (doc.RootElement.TryGetProperty("embedding", out var embeddingProp))
            {
                // Ollama old /api/embeddings format
                var vec = new float[embeddingProp.GetArrayLength()];
                int i = 0;
                foreach (var num in embeddingProp.EnumerateArray())
                    vec[i++] = (float)num.GetDouble();
                return new List<float[]> { vec };
            }

            return new List<float[]>();
        }
    }
}
