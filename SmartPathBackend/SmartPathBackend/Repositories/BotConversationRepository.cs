using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class BotConversationRepository : BaseRepository<BotConversation>, IBotConversationRepository
    {
        private readonly SmartPathDbContext _db;

        public BotConversationRepository(SmartPathDbContext db) : base(db) => _db = db;

        public async Task<IReadOnlyList<BotConversation>> GetByOwnerAsync(Guid ownerId, int page, int pageSize)
        {
            var q = _db.BotConversations
                .Where(c => c.OwnerId == ownerId)
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking();

            return await q.ToListAsync();
        }

        public Task<int> CountByOwnerAsync(Guid ownerId)
            => _db.BotConversations.CountAsync(c => c.OwnerId == ownerId);

        public async Task<BotConversation?> GetWithMessagesAsync(Guid id, Guid ownerId, int? take = null, Guid? beforeMessageId = null)
        {
            var convo = await _db.BotConversations
                .Where(c => c.Id == id && c.OwnerId == ownerId)
                .Include(c => c.Messages!.OrderByDescending(m => m.CreatedAt))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (convo == null || convo.Messages == null) return convo;

            var msgs = convo.Messages.AsQueryable();
            if (beforeMessageId.HasValue)
            {
                var before = convo.Messages.FirstOrDefault(m => m.Id == beforeMessageId.Value);
                if (before != null) msgs = msgs.Where(m => m.CreatedAt < before.CreatedAt);
            }
            if (take.HasValue) msgs = msgs.Take(take.Value);

            convo.Messages = msgs.OrderBy(m => m.CreatedAt).ToList();
            return convo;
        }

        public Task<int> TouchUpdatedAtAsync(Guid conversationId, DateTime now) =>
            _db.BotConversations
           .Where(c => c.Id == conversationId)
           .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, now));
    }
}
