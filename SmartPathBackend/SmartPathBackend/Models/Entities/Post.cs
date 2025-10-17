using System.Xml.Linq;

namespace SmartPathBackend.Models.Entities
{
    public class Post: BaseEntity
    {
        public Guid AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsQuestion { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? IsDeletedAt { get; set; }

        public User Author { get; set; } = null!;
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<CategoryPost>? CategoryPosts { get; set; }
        public ICollection<Reaction>? Reactions { get; set; }
    }
}
