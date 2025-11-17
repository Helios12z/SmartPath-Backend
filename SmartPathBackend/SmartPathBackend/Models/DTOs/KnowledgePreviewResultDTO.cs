namespace SmartPathBackend.Models.DTOs
{
    public class KnowledgePreviewResultDTO
    {
        public string ProposedTitle { get; set; } = default!;
        public string? ProposedSourceUrl { get; set; }
        public List<KnowledgeDocumentDto> RelatedDocuments { get; set; } = new();
    }
}
