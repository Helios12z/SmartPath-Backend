using SmartPathBackend.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartPathBackend.Models.Entities
{
    public class Post: BaseEntity
    {
        public Guid AuthorId { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;
        public bool IsQuestion { get; set; }

        public Status Status { get; set; } = Status.Accepted; // Default to Accepted for backward compatibility
        public string? RejectReason { get; set; }

        // AI Review Fields
        public double? AiConfidence { get; set; }
        public bool? AiCategoryMatch { get; set; }
        public Guid? AiSuggestedCategoryId { get; set; }
        public string? AiReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? IsDeletedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByAdminId { get; set; }

        public User Author { get; set; } = null!;
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<CategoryPost>? CategoryPosts { get; set; }
        public ICollection<Reaction>? Reactions { get; set; }
    }
}
