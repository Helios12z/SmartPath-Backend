using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IReactionService
    {
        Task<ReactionResponseDto> ReactAsync(Guid userId, ReactionRequestDto request);
        Task<bool> RemovePostReactionAsync(Guid userId, Guid postId);
        Task<bool> RemoveCommentReactionAsync(Guid userId, Guid commentId);
    }
}
