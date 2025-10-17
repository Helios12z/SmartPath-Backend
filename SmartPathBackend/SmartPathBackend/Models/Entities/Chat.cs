namespace SmartPathBackend.Models.Entities
{
    public class Chat: BaseEntity
    {
        public string? Name { get; set; }
        public Guid Member1Id { get; set; }
        public Guid Member2Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Message>? Messages { get; set; }
    }
}
