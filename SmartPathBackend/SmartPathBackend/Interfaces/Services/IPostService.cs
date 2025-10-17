using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IPostService
    {
        Task<IEnumerable<PostResponseDto>> GetAllAsync();
        Task<PostResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<PostResponseDto>> GetByUserAsync(Guid userId);
        Task<PostResponseDto> CreateAsync(Guid authorId, PostRequestDto request);
        Task<PostResponseDto?> UpdateAsync(Guid postId, PostRequestDto request);
        Task<bool> DeleteAsync(Guid postId);
    }
}
