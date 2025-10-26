using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class BotMessageRepository : BaseRepository<BotMessage>, IBotMessageRepository
    {
        private readonly SmartPathDbContext _db;

        public BotMessageRepository(SmartPathDbContext db) : base(db) =>_db = db;

        public async Task<IReadOnlyList<BotMessage>> GetByConversationAsync(Guid conversationId, int limit, Guid? beforeMessageId)
        {
            IQueryable<BotMessage> q = _db.BotMessages.Where(m => m.ConversationId == conversationId);

            if (beforeMessageId.HasValue)
            {
                var before = await _db.BotMessages.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == beforeMessageId.Value);

                if (before != null)
                {
                    q = q.Where(m => m.CreatedAt < before.CreatedAt);
                }
            }

            q = q.OrderByDescending(m => m.CreatedAt);

            var list = await q.Take(limit).AsNoTracking().ToListAsync();

            return list.OrderBy(m => m.CreatedAt).ToList();
        }

        public Task<int> CountByConversationAsync(Guid conversationId)
            => _db.BotMessages.CountAsync(m => m.ConversationId == conversationId);

        public async Task<bool> DeleteByConversationAsync(Guid conversationId)
        {
            var rows = await _db.BotMessages.Where(m => m.ConversationId == conversationId).ExecuteDeleteAsync();
            return rows >= 0;
        }
    }
}
