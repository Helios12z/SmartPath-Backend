using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class FriendshipRepository : BaseRepository<Friendship>, IFriendshipRepository
    {
        public FriendshipRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId)
            => await _dbSet.Where(f => f.FollowerId == userId || f.FollowedUserId == userId)
                           .ToListAsync();

        public async Task<Friendship?> GetRelationshipAsync(Guid userAId, Guid userBId)
            => await _dbSet.FirstOrDefaultAsync(f =>
                (f.FollowerId == userAId && f.FollowedUserId == userBId) ||
                (f.FollowerId == userBId && f.FollowedUserId == userAId));
    }
}
