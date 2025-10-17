namespace SmartPathBackend.Models.DTOs
{
    public class ChatResponseDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public List<MessageResponseDto> Messages { get; set; } = new();
    }
}
