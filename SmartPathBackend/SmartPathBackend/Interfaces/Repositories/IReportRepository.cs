using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IReportRepository : IBaseRepository<Report>
    {
        Task<IEnumerable<Report>> GetPendingReportsAsync();
        Task<IEnumerable<Report>> GetReportsByUserAsync(Guid reporterId);
    }
}
