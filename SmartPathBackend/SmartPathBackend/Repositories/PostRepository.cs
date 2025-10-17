using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class PostRepository : BaseRepository<Post>, IPostRepository
    {
        public PostRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Post>> GetPostsByUserAsync(Guid userId) =>
            await _dbSet.Include(p => p.Author)
                        .Where(p => p.AuthorId == userId)
                        .ToListAsync();

        public async Task<IEnumerable<Post>> GetByCategoryAsync(Guid categoryId) =>
            await _dbSet.Include(p => p.CategoryPosts!)
                        .ThenInclude(cp => cp.Category)
                        .Where(p => p.CategoryPosts!.Any(cp => cp.CategoryId == categoryId))
                        .ToListAsync();

        public async Task<IEnumerable<Post>> GetRecentAsync(int limit = 10) =>
            await _dbSet.OrderByDescending(p => p.CreatedAt)
                        .Take(limit)
                        .Include(p => p.Author)
                        .ToListAsync();
    }
}
