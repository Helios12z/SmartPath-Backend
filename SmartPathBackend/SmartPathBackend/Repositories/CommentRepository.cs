using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class CommentRepository : BaseRepository<Comment>, ICommentRepository
    {
        public CommentRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Comment>> GetByPostAsync(Guid postId) =>
            await _dbSet.Include(c => c.Author)
                        .Include(c=> c.Reactions)
                        .Include(c => c.Replies)
                        .Where(c => c.PostId == postId)
                        .ToListAsync();

        public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId) =>
            await _dbSet.Include(c => c.Author)
                        .Where(c => c.ParentCommentId == parentCommentId)
                        .ToListAsync();

        public async Task<IEnumerable<DailyCountDto>> CountCreatedDailyAsync(DateTime startInclusive)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => c.CreatedAt >= startInclusive)
                .GroupBy(c => c.CreatedAt.Date)                
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
