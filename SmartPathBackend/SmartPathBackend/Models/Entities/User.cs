using Microsoft.Extensions.Hosting;
using SmartPathBackend.Models.Enums;
using System.Xml.Linq;

namespace SmartPathBackend.Models.Entities
{
    public class User: BaseEntity
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public string? Major { get; set; }
        public string? Faculty { get; set; }
        public int? YearOfStudy { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public Role Role { get; set; } = Role.Student;
        public int Point { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Post>? Posts { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<Reaction>? Reactions { get; set; }
        public ICollection<Friendship>? Friendships { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
