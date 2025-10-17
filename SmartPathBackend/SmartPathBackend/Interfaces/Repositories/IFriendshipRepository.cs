using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IFriendshipRepository : IBaseRepository<Friendship>
    {
        Task<IEnumerable<Friendship>> GetFriendsAsync(Guid userId);
        Task<Friendship?> GetRelationshipAsync(Guid userAId, Guid userBId);
    }
}
