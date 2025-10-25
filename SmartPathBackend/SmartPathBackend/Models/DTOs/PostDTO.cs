namespace SmartPathBackend.Models.DTOs
{
    public class PostRequestDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsQuestion { get; set; } = false;
        public List<Guid>? CategoryIds { get; set; }
    }

    public class PostResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsQuestion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? AuthorUsername { get; set; } 
        public Guid AuthorId { get; set; } 
        public string? AuthorAvatarUrl { get; set; }
        public int PositiveReactionCount { get; set; }
        public int NegativeReactionCount { get; set; }
        public int CommentCount { get; set; }
        public List<string>? Categories { get; set; }
        public bool? IsPositiveReacted { get; set; }
        public bool? IsNegativeReacted { get; set; }
    }
}
