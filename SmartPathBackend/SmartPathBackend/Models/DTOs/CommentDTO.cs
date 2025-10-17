namespace SmartPathBackend.Models.DTOs
{
    public class CommentRequestDto
    {
        public Guid PostId { get; set; }
        public string Content { get; set; } = null!;
        public Guid? ParentCommentId { get; set; }
    }

    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public string AuthorUsername { get; set; } = null!;
        public string? AuthorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CommentResponseDto>? Replies { get; set; }
    }
}
