namespace SmartPathBackend.Models.DTOs
{
    public class BotGenerateRequest
    {
        public Guid ConversationId { get; set; }
        public string UserContent { get; set; } = null!;
        public string? SystemPrompt { get; set; }     
        public int? ContextLimit { get; set; }        
        public string? Model { get; set; }            
    }

    public class BotGenerateResponse
    {
        public BotMessageResponse UserMessage { get; set; } = null!;
        public BotMessageResponse AssistantMessage { get; set; } = null!;
    }
}
