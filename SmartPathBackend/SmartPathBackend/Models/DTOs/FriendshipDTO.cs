namespace SmartPathBackend.Models.DTOs
{
    public class FriendshipRequestDto
    {
        public Guid FollowedUserId { get; set; }
    }

    public class FriendshipResponseDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
        public Guid FollowerId { get; set; }
        public Guid FollowedUserId { get; set; }
    }
}
