using AutoMapper;
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

        public CommentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommentResponseDto>> GetByPostAsync(Guid postId, Guid? currentUserId)
        {
            var list = await _unitOfWork.Comments.GetByPostAsync(postId);

            CommentResponseDto Map(Comment c) => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                AuthorId = c.AuthorId,
                AuthorUsername = c.Author.Username,
                AuthorAvatarUrl = c.Author.AvatarUrl,
                AuthorPoint = c.Author.Point,
                CreatedAt = c.CreatedAt,

                IsPositiveReacted = currentUserId.HasValue
                    ? c.Reactions!.Any(r => r.UserId == currentUserId && r.IsPositive)
                    : (bool?)null,

                IsNegativeReacted = currentUserId.HasValue
                    ? c.Reactions!.Any(r => r.UserId == currentUserId && !r.IsPositive)
                    : (bool?)null,

                Replies = c.Replies != null
                    ? c.Replies.Select(Map).ToList()
                    : new List<CommentResponseDto>()
            };

            return list.Select(Map);
        }

        public async Task<CommentResponseDto> CreateAsync(Guid authorId, CommentRequestDto request)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                PostId = request.PostId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();
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
