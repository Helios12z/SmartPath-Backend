using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IPostRepository : IBaseRepository<Post>
    {
        Task<IEnumerable<Post>> GetPostsByUserAsync(Guid userId);
        Task<IEnumerable<Post>> GetByCategoryAsync(Guid categoryId);
        Task<IEnumerable<Post>> GetRecentAsync(int limit = 10);
        IQueryable<Post> Query();
    }
}
