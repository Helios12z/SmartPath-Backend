using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IBotMessageRepository : IBaseRepository<BotMessage>
    {
        Task<IReadOnlyList<BotMessage>> GetByConversationAsync(Guid conversationId, int limit, Guid? beforeMessageId);
        Task<int> CountByConversationAsync(Guid conversationId);
        Task<bool> DeleteByConversationAsync(Guid conversationId);
    }
}
