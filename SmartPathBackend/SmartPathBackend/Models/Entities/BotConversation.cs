namespace SmartPathBackend.Models.Entities
{
    public class BotConversation : BaseEntity
    {
        public Guid OwnerId { get; set; }
        public string? Title { get; set; }            
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public User Owner { get; set; } = null!;
        public ICollection<BotMessage>? Messages { get; set; }
    }
}
