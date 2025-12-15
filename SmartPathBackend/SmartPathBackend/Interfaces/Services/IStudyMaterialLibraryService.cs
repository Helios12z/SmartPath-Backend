using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IStudyMaterialLibraryService
    {
        Task<StudyMaterialResponse> CreateAsync(Guid uploaderId, StudyMaterialCreateMeta meta, IFormFile? file, CancellationToken ct);
        Task<StudyMaterialResponse?> GetByIdAsync(Guid? requesterId, Guid id);
        Task<(List<StudyMaterialResponse> Items, int Total)> SearchAsync(Guid? categoryId, Status? status, string? q, int page, int pageSize);
        Task<(List<StudyMaterialResponse> Items, int Total)> GetMineAsync(Guid uploaderId, Status? status, int page, int pageSize);

        Task<bool> AdminReviewAsync(Guid adminId, Guid materialId, ReviewDecisionRequest req);

        // Rating system methods
        Task<StudyMaterialRatingStats> GetRatingStatsAsync(Guid materialId);
        Task<StudyMaterialRatingResponse?> RateMaterialAsync(Guid userId, Guid materialId, StudyMaterialRatingRequest request);
        Task<List<StudyMaterialRatingResponse>> GetMaterialRatingsAsync(Guid materialId, int page = 1, int pageSize = 20);
        Task<StudyMaterialRatingResponse?> GetUserRatingAsync(Guid userId, Guid materialId);
        Task<bool> DeleteRatingAsync(Guid userId, Guid materialId);
    }
}
