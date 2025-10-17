using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentResponseDto>> GetByPostAsync(Guid postId);
        Task<CommentResponseDto> CreateAsync(Guid authorId, CommentRequestDto request);
        Task<CommentResponseDto?> UpdateAsync(Guid commentId, CommentRequestDto request);
        Task<bool> DeleteAsync(Guid commentId);
    }
}
