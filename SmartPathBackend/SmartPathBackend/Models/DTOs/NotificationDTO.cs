namespace SmartPathBackend.Models.DTOs
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Url { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
