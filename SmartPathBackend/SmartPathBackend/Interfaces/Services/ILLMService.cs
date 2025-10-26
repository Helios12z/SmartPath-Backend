namespace SmartPathBackend.Interfaces.Services
{
    public interface ILLMService
    {
        Task<string> CompleteAsync(
            string? systemPrompt,
            IEnumerable<(string role, string content)> messages,
            string? modelOverride = null,
            CancellationToken ct = default);
    }
}
