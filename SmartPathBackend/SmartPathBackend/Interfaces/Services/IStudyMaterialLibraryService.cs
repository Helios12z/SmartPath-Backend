using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IStudyMaterialLibraryService
    {
        Task<StudyMaterialResponse> CreateAsync(Guid uploaderId, StudyMaterialCreateMeta meta, IFormFile? file, CancellationToken ct);
        Task<StudyMaterialResponse?> GetByIdAsync(Guid? requesterId, Guid id);
        Task<List<StudyMaterialResponse>> SearchAsync(Guid? categoryId, Status? status, string? q);
        Task<List<StudyMaterialResponse>> GetMineAsync(Guid uploaderId, Status? status);

        Task<bool> AdminReviewAsync(Guid adminId, Guid materialId, ReviewDecisionRequest req);

        // Rating system methods
        Task<StudyMaterialRatingStats> GetRatingStatsAsync(Guid materialId);
        Task<StudyMaterialRatingResponse?> RateMaterialAsync(Guid userId, Guid materialId, StudyMaterialRatingRequest request);
        Task<List<StudyMaterialRatingResponse>> GetMaterialRatingsAsync(Guid materialId);
        Task<StudyMaterialRatingResponse?> GetUserRatingAsync(Guid userId, Guid materialId);
        Task<bool> DeleteRatingAsync(Guid userId, Guid materialId);
    }
}
