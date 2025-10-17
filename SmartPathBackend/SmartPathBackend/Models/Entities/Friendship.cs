using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    public class Friendship : BaseEntity
    {
        public Guid FollowerId { get; set; }
        public Guid FollowedUserId { get; set; }
        public Status Status { get; set; } = Status.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User Follower { get; set; } = null!;
        public User FollowedUser { get; set; } = null!;
    }
}
