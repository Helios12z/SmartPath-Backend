using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IChatRepository : IBaseRepository<Chat>
    {
        Task<Chat?> GetDirectChatAsync(Guid member1Id, Guid member2Id);
        Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId);
    }
}
