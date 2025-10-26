using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IBotConversationRepository : IBaseRepository<BotConversation>
    {
        Task<IReadOnlyList<BotConversation>> GetByOwnerAsync(Guid ownerId, int page, int pageSize);
        Task<int> CountByOwnerAsync(Guid ownerId);
        Task<BotConversation?> GetWithMessagesAsync(Guid id, Guid ownerId, int? take = null, Guid? beforeMessageId = null);
        Task<int> TouchUpdatedAtAsync(Guid conversationId, DateTime now);
    }
}
