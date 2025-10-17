using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class ReactionService : IReactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReactionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ReactionResponseDto> ReactAsync(Guid userId, ReactionRequestDto request)
        {
            var existing = await _unitOfWork.Reactions.GetUserReactionAsync(request.PostId, userId);
            if (existing != null)
            {
                existing.IsPositive = request.IsPositive;
                _unitOfWork.Reactions.Update(existing);
                await _unitOfWork.SaveChangesAsync();
                return _mapper.Map<ReactionResponseDto>(existing);
            }

            var reaction = new Reaction
            {
                Id = Guid.NewGuid(),
                PostId = request.PostId,
                UserId = userId,
                IsPositive = request.IsPositive,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Reactions.AddAsync(reaction);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReactionResponseDto>(reaction);
        }

        public async Task<bool> RemoveReactionAsync(Guid postId, Guid userId)
        {
            var reaction = await _unitOfWork.Reactions.GetUserReactionAsync(postId, userId);
            if (reaction == null) return false;

            _unitOfWork.Reactions.Remove(reaction);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
