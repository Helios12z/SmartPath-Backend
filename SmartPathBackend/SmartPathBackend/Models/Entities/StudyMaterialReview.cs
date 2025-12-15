using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    public class StudyMaterialReview: BaseEntity
    {
        public Guid MaterialId { get; set; }
        public StudyMaterial? Material { get; set; }

        public string ReviewerType { get; set; } = default!; 
        public Status Decision { get; set; } 
        public double? Confidence { get; set; }
        public string? Reason { get; set; }
        public string? RawResponse { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? AdminId { get; set; }
    }
}
