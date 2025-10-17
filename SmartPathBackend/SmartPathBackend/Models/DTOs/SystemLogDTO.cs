namespace SmartPathBackend.Models.DTOs
{
    public class SystemLogResponseDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
