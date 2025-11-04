namespace SmartPathBackend.Interfaces.Services
{
    public interface IEmbedderService
    {
        Task<float[]> EmbedOneAsync(string input, CancellationToken ct = default);
        Task<List<float[]>> EmbedManyAsync(IEnumerable<string> inputs, CancellationToken ct = default);
    }
}
