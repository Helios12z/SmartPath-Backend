using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders.Physical;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notifications;

        public CommentService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notifications)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notifications = notifications;
        }

        public async Task<IEnumerable<CommentResponseDto>> GetByPostAsync(Guid postId, Guid? currentUserId)
        {
            // Get ALL top-level comments (no parent)
            var topLevelComments = await _unitOfWork.Comments.Query()
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .Include(c => c.Author)
                .Include(c => c.Reactions)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // Get ALL replies for this post (not paginated) - include all nested replies
            var allReplies = await _unitOfWork.Comments.Query()
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.ParentCommentId.HasValue)
                .Include(c => c.Author)
                .Include(c => c.Reactions)
                .ToListAsync();

            // Group replies by parent for easy lookup
            var repliesByParent = allReplies.GroupBy(r => r.ParentCommentId)
                .ToDictionary(g => g.Key!.Value, g => g.ToList());

            // Recursive function to build complete comment tree including all nested replies
            List<CommentResponseDto> BuildCommentTree(Comment comment, int depth = 0)
            {
                // Prevent infinite recursion with depth limit (though in practice shouldn't be needed)
                if (depth > 10) return new List<CommentResponseDto>();

                var dto = new CommentResponseDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    AuthorId = comment.AuthorId,
                    AuthorUsername = comment.Author.Username,
                    AuthorAvatarUrl = comment.Author.AvatarUrl,
                    AuthorPoint = comment.Author.Point,
                    CreatedAt = comment.CreatedAt,

                    PositiveReactionCount = comment.Reactions != null ? comment.Reactions.Count(r => r.IsPositive) : 0,
                    NegativeReactionCount = comment.Reactions != null ? comment.Reactions.Count(r => !r.IsPositive) : 0,

                    IsPositiveReacted = currentUserId.HasValue
                        ? comment.Reactions!.Any(r => r.UserId == currentUserId && r.IsPositive)
                        : (bool?)null,

                    IsNegativeReacted = currentUserId.HasValue
                        ? comment.Reactions!.Any(r => r.UserId == currentUserId && !r.IsPositive)
                        : (bool?)null,

                    // Recursively get ALL child replies (not paginated) - each child builds its own tree
                    Replies = repliesByParent.ContainsKey(comment.Id)
                        ? repliesByParent[comment.Id]
                            .SelectMany(child => BuildCommentTree(child, depth + 1))
                            .OrderBy(r => r.CreatedAt)
                            .ToList()
                        : new List<CommentResponseDto>()
                };

                return new List<CommentResponseDto> { dto };
            }

            // Build complete comment trees for all top-level comments
            var items = new List<CommentResponseDto>();
            foreach (var topLevelComment in topLevelComments)
            {
                items.AddRange(BuildCommentTree(topLevelComment));
            }

            return items;
        }

        public async Task<CommentResponseDto> CreateAsync(Guid authorId, CommentRequestDto request)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                PostId = request.PostId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                ParentCommentId = request.ParentCommentId
            };
            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            var url = $"/posts/{comment.PostId}?c={comment.Id}";

            if (request.ParentCommentId.HasValue)
            {
                var parent = await _unitOfWork.Comments.GetByIdAsync(request.ParentCommentId.Value);
                if (parent != null && parent.AuthorId != authorId)
                {
                    await _notifications.CreateAsync(
                        receiverId: parent.AuthorId,
                        type: "comment.reply",
                        content: "Bình luận của bạn vừa có phản hồi.",
                        url: url
                    );
                }
            }

            var post = await _unitOfWork.Posts.GetByIdAsync(comment.PostId);
            if (post != null && post.AuthorId != authorId && !request.ParentCommentId.HasValue)
            {
                await _notifications.CreateAsync(
                    receiverId: post.AuthorId,
                    type: "comment.on_post",
                    content: "Bài viết của bạn vừa có bình luận mới.",
                    url: url
                );
            }

            return _mapper.Map<CommentResponseDto>(comment);
        }

        public async Task<CommentResponseDto?> UpdateAsync(Guid commentId, CommentRequestDto request)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null) return null;

            comment.Content = request.Content;
            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CommentResponseDto>(comment);
        }

        public async Task<bool> DeleteAsync(Guid commentId)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null) return false;

            _unitOfWork.Comments.Remove(comment);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
