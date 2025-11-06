namespace SmartPathBackend.Models.DTOs
{
    public class ChatOtherUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }

    public class ChatResponseDto
    {
        public Guid Id { get; set; }

        public Guid Member1Id { get; set; }
        public Guid Member2Id { get; set; }

        public ChatOtherUserDto? OtherUser { get; set; }  
        public List<MessageResponseDto> Messages { get; set; } = new();
    }
}
