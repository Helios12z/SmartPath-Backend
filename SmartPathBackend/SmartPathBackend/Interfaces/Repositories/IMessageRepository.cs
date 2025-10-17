using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IMessageRepository : IBaseRepository<Message>
    {
        Task<IEnumerable<Message>> GetMessagesByChatAsync(Guid chatId);
        Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid userId);
    }
}
