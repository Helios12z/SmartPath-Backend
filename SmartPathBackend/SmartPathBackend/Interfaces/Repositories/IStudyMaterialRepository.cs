using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IStudyMaterialRepository : IBaseRepository<StudyMaterial>
    {
        Task<StudyMaterial?> GetWithCategoryAsync(Guid id);
        Task<(List<StudyMaterial> Items, int Total)> SearchAsync(
            Guid? categoryId, Status? status, string? q, int page, int pageSize);
        Task<(List<StudyMaterial> Items, int Total)> GetMineAsync(
            Guid uploaderId, Status? status, int page, int pageSize);
    }
}
