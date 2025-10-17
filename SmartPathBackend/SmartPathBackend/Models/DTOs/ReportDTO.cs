namespace SmartPathBackend.Models.DTOs
{
    public class ReportRequestDto
    {
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? ReportedUserId { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class ReportResponseDto
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
