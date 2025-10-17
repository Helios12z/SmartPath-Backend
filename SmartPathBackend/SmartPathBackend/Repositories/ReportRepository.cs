using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Repositories
{
    public class ReportRepository : BaseRepository<Report>, IReportRepository
    {
        public ReportRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Report>> GetPendingReportsAsync()
            => await _dbSet.Include(r => r.Reporter)
                           .Where(r => r.Status == Status.Pending)
                           .ToListAsync();

        public async Task<IEnumerable<Report>> GetReportsByUserAsync(Guid reporterId)
            => await _dbSet.Where(r => r.ReporterId == reporterId)
                           .ToListAsync();
    }
}
