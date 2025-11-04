using Pgvector;

namespace SmartPathBackend.Models.Entities
{
    public class KnowledgeChunk
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public KnowledgeDocument Document { get; set; } = null!;
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = "";

        public Vector Embedding { get; set; } = new Vector(Array.Empty<float>());

        public DateTime CreatedAt { get; set; }
    }
}
