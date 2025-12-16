using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IPostService
    {
        Task<(IEnumerable<PostResponseDto> Items, int Total)> GetAllAsync(Guid? currentUserId, int page = 1, int pageSize = 20);
        Task<PostResponseDto?> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<(IEnumerable<PostResponseDto> Items, int Total)> GetByUserAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<PostResponseDto>> GetRecommendationsAsync(Guid? currentUserId, int? limit = null);
        Task<PostResponseDto> CreateAsync(Guid authorId, PostRequestDto request);
        Task<PostResponseDto?> UpdateAsync(Guid postId, PostRequestDto request, Guid? currentUserId);
        Task<bool> DeleteAsync(Guid postId);
    }
}
