using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    [Table("StudyMaterialSearchIndices")]
    public class StudyMaterialSearchIndex
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid StudyMaterialId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
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

        public int CategoryLevel { get; set; }

        public string Tags { get; set; } = string.Empty; // JSON array of tags

        public bool IsApproved { get; set; }

        public float AiConfidence { get; set; }

        public string? AiReason { get; set; }

        [NotMapped]
        public float[]? Embedding { get; set; }

        public DateTime LastIndexedAt { get; set; }

        public int Version { get; set; } = 1;

        [NotMapped]
        public List<string> TagList => string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags);

        // Navigation properties
        [ForeignKey("StudyMaterialId")]
        public virtual StudyMaterial StudyMaterial { get; set; } = null!;

        [ForeignKey("UploaderId")]
        public virtual User Uploader { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual MaterialCategory Category { get; set; } = null!;
    }
}