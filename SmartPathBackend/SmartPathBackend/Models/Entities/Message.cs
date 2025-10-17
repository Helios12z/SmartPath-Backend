namespace SmartPathBackend.Models.Entities
{
    public class Message: BaseEntity
    {
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public Chat Chat { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}
