using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    public class StudyMaterialRating : BaseEntity
    {
        public Guid MaterialId { get; set; }
        public StudyMaterial? Material { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public int Rating { get; set; } // 1-5 stars
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}