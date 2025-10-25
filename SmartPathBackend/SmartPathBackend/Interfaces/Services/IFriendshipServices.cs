using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IFriendshipService
    {
        Task<IEnumerable<FriendshipResponseDto>> GetFriendsAsync(Guid userId);
        Task<FriendshipResponseDto?> AddFriendAsync(Guid followerId, FriendshipRequestDto request);
        Task<bool> CancelFollowAsync(Guid followerId, Guid followedUserId);
        Task<FriendshipResponseDto?> AcceptAsync(Guid actingUserId, Guid friendshipId);
        Task<bool> RejectAsync(Guid actingUserId, Guid friendshipId);
    }
}
