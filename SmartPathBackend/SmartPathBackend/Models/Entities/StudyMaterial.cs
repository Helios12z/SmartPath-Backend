using SmartPathBackend.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartPathBackend.Models.Entities
{
    public class StudyMaterial: BaseEntity
    {
        public Guid UploaderId { get; set; }
        public User? Uploader { get; set; }

        public Guid CategoryId { get; set; }
        public MaterialCategory? Category { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; } = default!;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public StudyMaterialSourceType SourceType { get; set; }

        [MaxLength(2000)]
        public string? FileUrl { get; set; }
        [MaxLength(2000)]
        public string? SourceUrl { get; set; }

        [MaxLength(200)]
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }

        public Status Status { get; set; } = Status.Pending; 
        public string? RejectReason { get; set; }

        public double? AiConfidence { get; set; }
        public bool? AiCategoryMatch { get; set; }
        public Guid? AiSuggestedCategoryId { get; set; }
        public string? AiReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByAdminId { get; set; }
    }
}
