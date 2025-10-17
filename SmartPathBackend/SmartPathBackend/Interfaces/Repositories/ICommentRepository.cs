using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface ICommentRepository : IBaseRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetByPostAsync(Guid postId);
        Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId);
    }
}
