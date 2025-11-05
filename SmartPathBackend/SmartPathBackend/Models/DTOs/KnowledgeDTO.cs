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

    public class KnowledgeDocumentDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? SourceUrl { get; set; }
        public string? Meta { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ChunkCount { get; set; }
    }

    public class KnowledgeDocumentUpdateRequest
    {
        public string? Title { get; set; }
        public string? Meta { get; set; }
    }

    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    }
}
