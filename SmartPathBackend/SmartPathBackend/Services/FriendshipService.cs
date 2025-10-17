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

        public FriendshipService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

            return _mapper.Map<FriendshipResponseDto>(friendship);
        }
    }
}
