using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IReactionService
    {
        Task<ReactionResponseDto> ReactAsync(Guid userId, ReactionRequestDto request);
        Task<bool> RemoveReactionAsync(Guid postId, Guid userId);
    }
}
