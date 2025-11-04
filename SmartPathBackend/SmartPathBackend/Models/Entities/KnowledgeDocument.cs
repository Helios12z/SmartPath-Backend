namespace SmartPathBackend.Models.Entities
{
    public class KnowledgeDocument
    {
        public Guid Id { get; set; }
        public string? SourceUrl { get; set; }
        public string? Title { get; set; }
        public string? Meta { get; set; } 
        public DateTime CreatedAt { get; set; }
        public List<KnowledgeChunk> Chunks { get; set; } = [];
    }
}
