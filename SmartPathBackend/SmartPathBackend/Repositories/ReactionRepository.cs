using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class ReactionRepository : BaseRepository<Reaction>, IReactionRepository
    {
        public ReactionRepository(SmartPathDbContext context) : base(context) { }

        public async Task<int> CountReactionsAsync(Guid postId, bool isPositive) =>
            await _dbSet.CountAsync(r => r.PostId == postId && r.IsPositive == isPositive);

        public async Task<Reaction?> GetUserReactionAsync(Guid postId, Guid userId) =>
            await _dbSet.FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);
    }
}
