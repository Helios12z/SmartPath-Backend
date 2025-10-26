using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IChatRepository : IBaseRepository<Chat>
    {
        Task<Chat?> GetDirectChatAsync(Guid member1Id, Guid member2Id);
        Task<Chat?> GetByIdWithMessagesAsync(Guid chatId);
        Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId);
        Task<IEnumerable<Chat>> GetChatsByUserWithMessagesAsync(Guid userId);
    }
}
