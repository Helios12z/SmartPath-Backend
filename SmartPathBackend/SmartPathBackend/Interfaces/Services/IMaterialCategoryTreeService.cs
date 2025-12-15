using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IMaterialCategoryTreeService
    {
        Task<List<MaterialCategoryNodeDto>> GetTreeAsync();
        Task<MaterialCategoryNodeDto> CreateAsync(Guid adminId, MaterialCategoryCreateRequest req);
        Task<MaterialCategoryNodeDto?> UpdateAsync(Guid adminId, Guid id, MaterialCategoryUpdateRequest req);
        Task<bool> DeleteAsync(Guid adminId, Guid id);
        Task<bool> MoveAsync(Guid adminId, Guid id, MoveCategoryRequest req);
    }
}
