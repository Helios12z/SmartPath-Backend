using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class CommentRepository : BaseRepository<Comment>, ICommentRepository
    {
        public CommentRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Comment>> GetByPostAsync(Guid postId) =>
            await _dbSet.Include(c => c.Author)
                        .Include(c => c.Replies)
                        .Where(c => c.PostId == postId)
                        .ToListAsync();

        public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId) =>
            await _dbSet.Include(c => c.Author)
                        .Where(c => c.ParentCommentId == parentCommentId)
                        .ToListAsync();
    }
}
