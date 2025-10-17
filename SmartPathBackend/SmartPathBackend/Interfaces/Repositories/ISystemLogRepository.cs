using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface ISystemLogRepository : IBaseRepository<SystemLog>
    {
        Task<IEnumerable<SystemLog>> GetByUserAsync(Guid userId);
        Task<IEnumerable<SystemLog>> GetRecentAsync(int limit = 50);
    }
}
