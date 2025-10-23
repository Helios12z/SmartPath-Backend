using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IMaterialRepository : IBaseRepository<Material>
    {
        Task<IReadOnlyList<Material>> GetByPostAsync(Guid postId);
        Task<IReadOnlyList<Material>> GetByCommentAsync(Guid commentId);
        Task<IReadOnlyList<Material>> GetByMessageAsync(Guid messageId);
    }
}
