using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPathBackend.Models.Entities
{
    [Table("SearchQueryLogs")]
    public class SearchQueryLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Query { get; set; } = string.Empty;

        public string NormalizedQuery { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public string? UserIdentifier { get; set; } // For anonymous users

        public string SearchType { get; set; } = string.Empty;

        public string Filters { get; set; } = string.Empty; // JSON representation of filters

        public int ResultCount { get; set; }

        public int PostResults { get; set; }

        public int StudyMaterialResults { get; set; }

        public TimeSpan QueryTime { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public string SortBy { get; set; } = string.Empty;

        public string SortOrder { get; set; } = string.Empty;

        public string UserAgent { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}