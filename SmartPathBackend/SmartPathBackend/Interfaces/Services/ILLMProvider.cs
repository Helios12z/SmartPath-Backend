namespace SmartPathBackend.Interfaces.Services
{
    public interface ILLMProvider
    {
        string Name { get; } 
        Task<string> CompleteAsync(
            string? systemPrompt,
            IEnumerable<(string role, string content)> messages,
            string? modelOverride = null,
            CancellationToken ct = default);
    }
}
