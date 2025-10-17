namespace SmartPathBackend.Models.DTOs
{
    public class MessageRequestDto
    {
        public Guid ChatId { get; set; }
        public string Content { get; set; } = null!;
    }

    public class MessageResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = null!;
        public Guid SenderId { get; set; }
        public string SenderUsername { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
