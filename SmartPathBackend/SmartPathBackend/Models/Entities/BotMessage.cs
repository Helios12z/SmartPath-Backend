using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    public class BotMessage : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }            
        public BotMessageRole Role { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public int? LatencyMs { get; set; }
        public string? ToolCallsJson { get; set; }    

        public BotConversation Conversation { get; set; } = null!;
    }
}
