using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IPostService
    {
        Task<IEnumerable<PostResponseDto>> GetAllAsync(Guid? currentUserId);
        Task<PostResponseDto?> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<IEnumerable<PostResponseDto>> GetByUserAsync(Guid userId);
        Task<PostResponseDto> CreateAsync(Guid authorId, PostRequestDto request);
        Task<PostResponseDto?> UpdateAsync(Guid postId, PostRequestDto request, Guid? currentUserId);
        Task<bool> DeleteAsync(Guid postId);
    }
}
