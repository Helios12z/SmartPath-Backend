namespace SmartPathBackend.Models.DTOs
{
    public class FileSummaryResult
    {
        public string ContentType { get; set; } = string.Empty;
        public bool IsTextBased { get; set; }
        public bool IsEducational { get; set; }
        public double QualityScore { get; set; }
        public string ExtractedSample { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public int PageCount { get; set; }
        public List<string> KeyTopics { get; set; } = new();
        public string AcademicLevel { get; set; } = string.Empty;
        public bool HasCode { get; set; }
        public bool HasFormulas { get; set; }
        public bool HasDiagrams { get; set; }
        public string Language { get; set; } = string.Empty;
        public int EstimatedReadingTime { get; set; }
        public string ProcessingNotes { get; set; } = string.Empty;
    }

    public class AIReviewDecisionResult
    {
        public bool CategoryMatch { get; set; }
        public bool IsAppropriate { get; set; }
        public double Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}