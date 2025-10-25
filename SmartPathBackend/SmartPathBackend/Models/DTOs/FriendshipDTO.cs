using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.DTOs
{
    public class FriendshipRequestDto
    {
        public Guid FollowedUserId { get; set; }
    }

    public class FriendshipResponseDto
    {
        public Guid Id { get; set; }
        public Status Status { get; set; } = Status.Pending;
        public Guid FollowerId { get; set; }
        public Guid FollowedUserId { get; set; }
    }
}
