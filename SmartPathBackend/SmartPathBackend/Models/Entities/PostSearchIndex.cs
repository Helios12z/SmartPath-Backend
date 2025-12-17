using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace SmartPathBackend.Models.Entities
{
    [Table("PostSearchIndices")]
    public class PostSearchIndex
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(1000)]
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

        public string CategoryIds { get; set; } = string.Empty; // JSON array of category IDs

        public string CategoryNames { get; set; } = string.Empty; // JSON array of category names

        public string CategorySlugs { get; set; } = string.Empty; // JSON array of category slugs

        public string Tags { get; set; } = string.Empty; // JSON array of tags

        public Vector? Embedding { get; set; }

        public DateTime LastIndexedAt { get; set; }

        public int Version { get; set; } = 1;

        [NotMapped]
        public List<Guid> CategoryIdList => string.IsNullOrEmpty(CategoryIds)
            ? new List<Guid>()
            : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(CategoryIds);

        [NotMapped]
        public List<string> CategoryNameList => string.IsNullOrEmpty(CategoryNames)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(CategoryNames);

        [NotMapped]
        public List<string> CategorySlugList => string.IsNullOrEmpty(CategorySlugs)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(CategorySlugs);

        [NotMapped]
        public List<string> TagList => string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags);

        // Navigation properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("AuthorId")]
        public virtual User Author { get; set; } = null!;
    }
}