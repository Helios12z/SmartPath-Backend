using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface ICategoryRepository : IBaseRepository<Category>
    {
        IQueryable<Category> Query();
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    }
}
