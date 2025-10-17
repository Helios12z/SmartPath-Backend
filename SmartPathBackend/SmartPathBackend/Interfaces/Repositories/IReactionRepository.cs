using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IReactionRepository : IBaseRepository<Reaction>
    {
        Task<int> CountReactionsAsync(Guid postId, bool isPositive);
        Task<Reaction?> GetUserReactionAsync(Guid postId, Guid userId);
    }
}
