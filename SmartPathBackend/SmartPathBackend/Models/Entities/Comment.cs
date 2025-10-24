namespace SmartPathBackend.Models.Entities
{
    public class Comment : BaseEntity
    {
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; } = null!;
        public Guid? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Post Post { get; set; } = null!;
        public User Author { get; set; } = null!;
        public Comment? ParentComment { get; set; }
        public ICollection<Comment>? Replies { get; set; }
        public ICollection<Reaction>? Reactions { get; set; }
    }
}
