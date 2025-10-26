using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.DTOs
{
    public class BotConversationCreateRequest
    {
        public string? Title { get; set; }            
        public string? SystemPrompt { get; set; }     
    }

    public class BotConversationResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class BotMessageRequest
    {
        public Guid ConversationId { get; set; }
        public string Content { get; set; } = null!;
        public BotMessageRole Role { get; set; } = BotMessageRole.User;

        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public int? LatencyMs { get; set; }
        public string? ToolCallsJson { get; set; }
    }

    public class BotMessageResponse
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public BotMessageRole Role { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public int? LatencyMs { get; set; }
        public string? ToolCallsJson { get; set; }
    }

    public class BotConversationWithMessagesResponse : BotConversationResponse
    {
        public List<BotMessageResponse> Messages { get; set; } = new();
    }

    public class RenameConversationRequest
    {
        public string Title { get; set; } = null!;
    }
}
