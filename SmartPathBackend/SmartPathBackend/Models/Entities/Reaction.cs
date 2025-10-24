namespace SmartPathBackend.Models.Entities
{
    public class Reaction : BaseEntity
    {
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid UserId { get; set; }
        public bool IsPositive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Post? Post { get; set; }
        public Comment? Comment { get; set; }
        public User User { get; set; } = null!;
    }
}
