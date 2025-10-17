using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartPathBackend.Repositories
{
    public class SystemLogRepository : BaseRepository<SystemLog>, ISystemLogRepository
    {
        public SystemLogRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<SystemLog>> GetByUserAsync(Guid userId)
            => await _dbSet.Where(l => l.UserId == userId)
                           .OrderByDescending(l => l.CreatedAt)
                           .ToListAsync();

        public async Task<IEnumerable<SystemLog>> GetRecentAsync(int limit = 50)
            => await _dbSet.OrderByDescending(l => l.CreatedAt)
                           .Take(limit)
                           .ToListAsync();
    }
}
