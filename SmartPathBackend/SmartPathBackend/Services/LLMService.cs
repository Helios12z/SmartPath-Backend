using Microsoft.Extensions.Options;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Utils;

namespace SmartPathBackend.Services
{
    public class LLMService : ILLMService
    {
        private readonly IEnumerable<ILLMProvider> _providers;
        private readonly LLMOptions _opt;

        public LLMService(IEnumerable<ILLMProvider> providers, IOptions<LLMOptions> opt)
        {
            _providers = providers;
            _opt = opt.Value;
        }

        public Task<string> CompleteAsync(
            string? systemPrompt,
            IEnumerable<(string role, string content)> messages,
            string? modelOverride = null,
            CancellationToken ct = default)
        {
            var providerName = _opt.Provider?.Trim() ?? "Gemini";
            var provider = _providers.FirstOrDefault(p =>
                string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase))
                ?? _providers.First(); 

            return provider.CompleteAsync(systemPrompt, messages, modelOverride, ct);
        }
    }
}
