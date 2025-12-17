using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.DTOs
{
    public class SemanticPostSearchResult
    {
        public Guid PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public bool IsQuestion { get; set; }
        public bool IsSolved { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorUsername { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string CategoryIds { get; set; } = string.Empty;
        public string CategoryNames { get; set; } = string.Empty;
        public string CategorySlugs { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
    }

    public class SemanticMaterialSearchResult
    {
        public Guid StudyMaterialId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public StudyMaterialResourceType ResourceType { get; set; }
        public string Url { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int DownloadCount { get; set; }
        public float AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UploaderId { get; set; }
        public string UploaderName { get; set; } = string.Empty;
        public string UploaderUsername { get; set; } = string.Empty;
        public string UploaderAvatar { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryPath { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public float AiConfidence { get; set; }
        public double RelevanceScore { get; set; }
    }
}