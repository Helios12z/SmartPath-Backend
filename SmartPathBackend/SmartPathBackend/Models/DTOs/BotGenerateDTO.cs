namespace SmartPathBackend.Models.DTOs
{
    public class BotGenerateRequest
    {
        public Guid ConversationId { get; set; }
        public string UserContent { get; set; } = null!;
        public string? SystemPrompt { get; set; }     
        public int? ContextLimit { get; set; }        
        public string? Model { get; set; }
        public bool? UseRag { get; set; } = true;     
        public float? MinSimilarity { get; set; }     
        public int? HistoryLimit { get; set; }       
        public double? Temperature { get; set; }      
        public int? MaxOutputTokens { get; set; }     
        public bool? Stream { get; set; }
    }

    public class BotGenerateResponse
    {
        public BotMessageResponse UserMessage { get; set; } = null!;
        public BotMessageResponse AssistantMessage { get; set; } = null!;
        public BotGenerateMeta Meta { get; set; } = new();
    }

    //for debugging and analysis
    public class BotGenerateMeta
    {
        public string? UsedModel { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public int? LatencyMs { get; set; }
        public int? RetrievedContextCount { get; set; }

        public IEnumerable<RetrievedContextPreview>? Contexts { get; set; }

        public IEnumerable<KnowledgeSourcePreview>? Sources { get; set; }
    }

    public class RetrievedContextPreview
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Snippet { get; set; } = "";     
    }

    public class KnowledgeSourcePreview
    {
        public Guid DocumentId { get; set; }
        public string? Title { get; set; }
        public string? SourceUrl { get; set; }
    }
}
