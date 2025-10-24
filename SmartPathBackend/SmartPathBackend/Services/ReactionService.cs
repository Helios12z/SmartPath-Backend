using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class ReactionService : IReactionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ReactionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _uow = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ReactionResponseDto> ReactAsync(Guid userId, ReactionRequestDto request)
        {
            if ((request.PostId.HasValue == request.CommentId.HasValue))
                throw new ArgumentException("Provide exactly one of PostId or CommentId.");

            var existing = await _uow.Reactions.GetUserReactionAsync(request.PostId, request.CommentId, userId);
            if (existing != null)
            {
                existing.IsPositive = request.IsPositive;
                _uow.Reactions.Update(existing);
                await _uow.SaveChangesAsync();
                return _mapper.Map<ReactionResponseDto>(existing);
            }

            var reaction = new Reaction
            {
                Id = Guid.NewGuid(),
                PostId = request.PostId,
                CommentId = request.CommentId,
                UserId = userId,
                IsPositive = request.IsPositive,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Reactions.AddAsync(reaction);
            await _uow.SaveChangesAsync();

            return _mapper.Map<ReactionResponseDto>(reaction);
        }

        public async Task<bool> RemoveReactionAsync(Guid userId, Guid? postId, Guid? commentId)
        {
            if ((postId.HasValue == commentId.HasValue))
                throw new ArgumentException("Provide exactly one of postId or commentId.");

            var reaction = await _uow.Reactions.GetUserReactionAsync(postId, commentId, userId);
            if (reaction == null) return false;

            _uow.Reactions.Remove(reaction);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
