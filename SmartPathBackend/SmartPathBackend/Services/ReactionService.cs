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
        private readonly INotificationService _notifications;

        public ReactionService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notifications)
        {
            _uow = unitOfWork;
            _mapper = mapper;
            _notifications = notifications;
        }

        public async Task<ReactionResponseDto> ReactAsync(Guid userId, ReactionRequestDto request)
        {
            var hasPost = request.PostId.HasValue;
            var hasComment = request.CommentId.HasValue;
            if (hasPost == hasComment)
                throw new ArgumentException("Provide exactly one of PostId or CommentId.");

            Reaction? existing = hasPost
                ? await _uow.Reactions.GetUserPostReactionAsync(request.PostId!.Value, userId)
                : await _uow.Reactions.GetUserCommentReactionAsync(request.CommentId!.Value, userId);

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

            var verb = request.IsPositive ? "được like" : "bị dislike";

            if (hasPost)
            {
                var post = await _uow.Posts.GetByIdAsync(request.PostId!.Value);
                if (post != null && post.AuthorId != userId)
                {
                    await _notifications.CreateAsync(
                        receiverId: post.AuthorId,
                        type: "reaction.post",
                        content: $"Bài viết của bạn {verb}.",
                        url: $"/posts/{post.Id}"
                    );
                }
            }
            else 
            {
                var cmt = await _uow.Comments.GetByIdAsync(request.CommentId!.Value);
                if (cmt != null && cmt.AuthorId != userId)
                {
                    var isReply = cmt.ParentCommentId.HasValue; 
                    var what = isReply ? "Phản hồi của bạn" : "Bình luận của bạn";

                    await _notifications.CreateAsync(
                        receiverId: cmt.AuthorId,
                        type: "reaction.comment",
                        content: $"{what} {verb}.",
                        url: $"/posts/{cmt.PostId}?c={cmt.Id}"
                    );
                }
            }

            return _mapper.Map<ReactionResponseDto>(reaction);
        }

        public async Task<bool> RemovePostReactionAsync(Guid userId, Guid postId)
        {
            var reaction = await _uow.Reactions.GetUserPostReactionAsync(postId, userId);
            if (reaction == null) return false;

            _uow.Reactions.Remove(reaction);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCommentReactionAsync(Guid userId, Guid commentId)
        {
            var reaction = await _uow.Reactions.GetUserCommentReactionAsync(commentId, userId);
            if (reaction == null) return false;

            _uow.Reactions.Remove(reaction);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
