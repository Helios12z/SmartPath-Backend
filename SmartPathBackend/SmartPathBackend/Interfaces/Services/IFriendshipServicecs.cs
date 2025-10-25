using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IFriendshipService
    {
        Task<IEnumerable<FriendshipResponseDto>> GetFriendsAsync(Guid userId);
        Task<FriendshipResponseDto?> AddFriendAsync(Guid followerId, FriendshipRequestDto request);
        Task<bool> CancelFollowAsync(Guid followerId, Guid followedUserId);
    }
}
