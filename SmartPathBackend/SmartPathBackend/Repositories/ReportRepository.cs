using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Repositories
{
    public class ReportRepository : BaseRepository<Report>, IReportRepository
    {
        private readonly SmartPathDbContext _db;

        public ReportRepository(SmartPathDbContext context) : base(context) 
        {
            _db = context;
        }

        public async Task<IEnumerable<Report>> GetPendingReportsAsync()
            => await _dbSet.Include(r => r.Reporter)
                           .Where(r => r.Status == Status.Pending)
                           .ToListAsync();

        public async Task<IEnumerable<Report>> GetReportsByUserAsync(Guid reporterId)
            => await _dbSet.Where(r => r.ReporterId == reporterId)
                           .ToListAsync();

        public async Task<int> CountFiledByAsync(Guid reporterId)
            => await _dbSet.CountAsync(r => r.ReporterId == reporterId);

        public async Task<int> CountAgainstUserContentAsync(Guid userId)
        {
            var postReports =
                from r in _dbSet
                join p in _db.Posts on r.PostId equals p.Id
                where p.AuthorId == userId
                select r.Id;

            var commentReports =
                from r in _dbSet
                join c in _db.Comments on r.CommentId equals c.Id
                where c.AuthorId == userId
                select r.Id;

            return await postReports.Concat(commentReports).Distinct().CountAsync();
        }

        public async Task<List<DailyCountDto>> CountCreatedDailyAsync(DateTime startInclusive)
        {
            return await _dbSet.AsNoTracking()
                .Where(r => r.CreatedAt >= startInclusive)
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new DailyCountDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }
    }
}
