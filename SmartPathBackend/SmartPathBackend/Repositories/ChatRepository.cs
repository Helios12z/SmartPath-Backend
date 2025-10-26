using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class ChatRepository : BaseRepository<Chat>, IChatRepository
    {
        public ChatRepository(SmartPathDbContext context) : base(context) { }

        public async Task<Chat?> GetDirectChatAsync(Guid member1Id, Guid member2Id) =>
            await _dbSet
                .FirstOrDefaultAsync(c =>
                    (c.Member1Id == member1Id && c.Member2Id == member2Id) ||
                    (c.Member1Id == member2Id && c.Member2Id == member1Id));

        public async Task<Chat?> GetByIdWithMessagesAsync(Guid chatId) =>
            await _dbSet
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId);

        public async Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId) =>
            await _dbSet
                .Where(c => c.Member1Id == userId || c.Member2Id == userId)
                .ToListAsync();

        public async Task<IEnumerable<Chat>> GetChatsByUserWithMessagesAsync(Guid userId) =>
            await _dbSet
                .Where(c => c.Member1Id == userId || c.Member2Id == userId)
                .Include(c => c.Messages)
                .ToListAsync();
    }
}
