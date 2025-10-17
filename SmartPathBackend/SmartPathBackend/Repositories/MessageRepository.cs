using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class MessageRepository : BaseRepository<Message>, IMessageRepository
    {
        public MessageRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Message>> GetMessagesByChatAsync(Guid chatId)
            => await _dbSet
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid userId)
            => await _dbSet
                .Include(m => m.Chat)
                .Where(m =>
                    !m.IsRead &&
                    (
                        (m.Chat.Member1Id == userId && m.SenderId != userId) ||
                        (m.Chat.Member2Id == userId && m.SenderId != userId)
                    ))
                .ToListAsync();
    }
}
