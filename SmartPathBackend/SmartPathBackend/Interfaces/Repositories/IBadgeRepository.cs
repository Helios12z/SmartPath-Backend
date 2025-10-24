using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IBadgeRepository: IBaseRepository<Badge>
    {
        IQueryable<Badge> Query();
        Task<bool> ExistsByNameOrPointAsync(string name, int point, Guid? excludeId = null, CancellationToken ct = default);
    }
}
