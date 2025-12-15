using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IMaterialCategoryRepository : IBaseRepository<MaterialCategory>
    {
        Task<List<MaterialCategory>> GetAllActiveAsync();
        Task<MaterialCategory?> GetBySlugAsync(string slug);
        Task<List<MaterialCategory>> GetTreeAsync(); 
    }
}
