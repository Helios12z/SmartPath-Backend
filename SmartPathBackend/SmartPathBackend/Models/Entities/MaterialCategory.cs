using System.ComponentModel.DataAnnotations;

namespace SmartPathBackend.Models.Entities
{
    public class MaterialCategory: BaseEntity
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = default!;

        [Required, MaxLength(200)]
        public string Slug { get; set; } = default!;

        public Guid? ParentId { get; set; }
        public MaterialCategory? Parent { get; set; }
        public ICollection<MaterialCategory> Children { get; set; } = new List<MaterialCategory>();

        [Required, MaxLength(800)]
        public string Path { get; set; } = default!; 

        public int Level { get; set; }
        public int SortOrder { get; set; } 

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
