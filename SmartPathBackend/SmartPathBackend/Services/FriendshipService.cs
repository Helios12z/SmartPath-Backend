using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notification; 

        public FriendshipService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notification)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notification = notification; 
        }

        public async Task<IEnumerable<FriendshipResponseDto>> GetFriendsAsync(Guid userId)
        {
            var friends = await _unitOfWork.Friendships.GetFriendsAsync(userId);
            return _mapper.Map<IEnumerable<FriendshipResponseDto>>(friends);
        }

        public async Task<FriendshipResponseDto?> AddFriendAsync(Guid followerId, FriendshipRequestDto request)
        {
            var existing = await _unitOfWork.Friendships.GetRelationshipAsync(followerId, request.FollowedUserId);
            if (existing != null)
                return _mapper.Map<FriendshipResponseDto>(existing);

            var friendship = new Friendship
            {
                Id = Guid.NewGuid(),
                FollowerId = followerId,
                FollowedUserId = request.FollowedUserId,
                Status = Status.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Friendships.AddAsync(friendship);
            await _unitOfWork.SaveChangesAsync();

            await _notification.CreateAsync(
                receiverId: request.FollowedUserId,
                type: "friend.request",
                content: "Bạn có một lời mời kết bạn.",
                url: "/friends"
            );

            return _mapper.Map<FriendshipResponseDto>(friendship);
        }

        public async Task<bool> CancelFollowAsync(Guid followerId, Guid followedUserId)
        {
            var relationship = await _unitOfWork.Friendships.GetRelationshipAsync(followerId, followedUserId);
            if (relationship is null) return false;
            if (relationship.FollowerId != followerId) return false;

            _unitOfWork.Friendships.Remove(relationship);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<FriendshipResponseDto?> AcceptAsync(Guid actingUserId, Guid friendshipId)
        {
            var relationship = await _unitOfWork.Friendships.GetByIdAsync(friendshipId);
            if (relationship is null) return null;

            if (relationship.FollowedUserId != actingUserId) return null;

            relationship.Status = Status.Accepted;
            _unitOfWork.Friendships.Update(relationship);
            await _unitOfWork.SaveChangesAsync();

            await _notification.CreateAsync(
                receiverId: relationship.FollowerId,
                type: "friend.accepted",
                content: "Yêu cầu kết bạn của bạn đã được chấp nhận.",
                url: $"/profile/{relationship.FollowedUserId}"
            );

            return _mapper.Map<FriendshipResponseDto>(relationship);
        }

        public async Task<bool> RejectAsync(Guid actingUserId, Guid friendshipId)
        {
            var relationship = await _unitOfWork.Friendships.GetByIdAsync(friendshipId);
            if (relationship is null) return false;

            if (relationship.FollowedUserId != actingUserId) return false;

            _unitOfWork.Friendships.Remove(relationship);
            await _unitOfWork.SaveChangesAsync();

            await _notification.CreateAsync(
                receiverId: relationship.FollowerId,
                type: "friend.rejected",
                content: "Yêu cầu kết bạn của bạn đã bị từ chối.",
                url: $"/profile/{relationship.FollowedUserId}"
            );

            return true;
        }
    }
}
