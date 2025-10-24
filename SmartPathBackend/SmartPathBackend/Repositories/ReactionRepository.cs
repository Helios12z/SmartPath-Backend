using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using System.ComponentModel.Design;

namespace SmartPathBackend.Repositories
{
    public class ReactionRepository : BaseRepository<Reaction>, IReactionRepository
    {
        public ReactionRepository(SmartPathDbContext context) : base(context) { }

        public async Task<int> CountReactionsAsync(Guid? postId, Guid? commentId, bool isPositive)
        {
            if ((postId.HasValue == commentId.HasValue))
                throw new ArgumentException("Provide exactly one of postId or commentId.");

            return await _dbSet
                .Where(r => r.IsPositive == isPositive
                    && (postId.HasValue ? r.PostId == postId : r.CommentId == commentId))
                .CountAsync();
        }

        public async Task<Reaction?> GetUserPostReactionAsync(Guid postId, Guid userId)
        {
            return await _dbSet.FirstOrDefaultAsync(r =>
                r.UserId == userId && r.PostId==postId);
        }

        public async Task<Reaction?> GetUserCommentReactionAsync(Guid commentId, Guid userId)
        {
            return await _dbSet.FirstOrDefaultAsync(r =>
                r.UserId == userId && r.CommentId==commentId);
        }
    }
}
