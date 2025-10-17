namespace SmartPathBackend.Models.Entities
{
    public class Material : BaseEntity
    {
        public Guid UploaderId { get; set; }
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? MessageId { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string FileUrl { get; set; } = null!;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
