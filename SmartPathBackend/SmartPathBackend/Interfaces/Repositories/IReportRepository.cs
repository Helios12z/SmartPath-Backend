using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IReportRepository : IBaseRepository<Report>
    {
        Task<IEnumerable<Report>> GetPendingReportsAsync();
        Task<IEnumerable<Report>> GetReportsByUserAsync(Guid reporterId);
        Task<int> CountFiledByAsync(Guid reporterId);
        Task<int> CountAgainstUserContentAsync(Guid userId); 
        Task<List<DailyCountDto>> CountCreatedDailyAsync(DateTime startInclusive);
    }
}
