namespace SmartPathBackend.Models.Entities
{
    public class Notification: BaseEntity
    {
        public Guid ReceiverId { get; set; }
        public string Type { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Url { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User Receiver { get; set; } = null!;
    }
}
