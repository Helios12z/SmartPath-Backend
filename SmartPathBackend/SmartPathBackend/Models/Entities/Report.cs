using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    public class Report : BaseEntity
    {
        public Guid ReporterId { get; set; }
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? ReportedUserId { get; set; }
        public string Reason { get; set; } = null!;
        public Status Status { get; set; } = Status.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? ReviewedBy { get; set; }

        public User Reporter { get; set; } = null!;
    }
}
