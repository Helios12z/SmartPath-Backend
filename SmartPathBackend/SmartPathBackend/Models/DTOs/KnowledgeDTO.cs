namespace SmartPathBackend.Models.DTOs
{
    public class KnowledgeSearchHit
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = "";
        public string? Title { get; set; }
        public string? SourceUrl { get; set; }
        public double Score { get; set; } 
    }
}
