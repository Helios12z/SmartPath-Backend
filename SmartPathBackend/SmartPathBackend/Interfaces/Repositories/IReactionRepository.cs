using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IReactionRepository : IBaseRepository<Reaction>
    {
        Task<int> CountReactionsAsync(Guid? postId, Guid? commentId, bool isPositive);
        Task<Reaction?> GetUserPostReactionAsync(Guid postId, Guid userId);
        Task<Reaction?> GetUserCommentReactionAsync(Guid commentId, Guid userId);
    }
}
