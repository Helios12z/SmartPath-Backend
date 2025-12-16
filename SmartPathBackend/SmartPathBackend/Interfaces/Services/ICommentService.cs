using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface ICommentService
    {
        Task<(IEnumerable<CommentResponseDto> Items, int Total)> GetByPostAsync(Guid postId, Guid? currentUserId, int page = 1, int pageSize = 20);
        Task<CommentResponseDto> CreateAsync(Guid authorId, CommentRequestDto request);
        Task<CommentResponseDto?> UpdateAsync(Guid commentId, CommentRequestDto request);
        Task<bool> DeleteAsync(Guid commentId);
    }
}
