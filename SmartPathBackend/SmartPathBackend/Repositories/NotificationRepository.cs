using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(SmartPathDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetByReceiverAsync(Guid receiverId) =>
            await _dbSet.Where(n => n.ReceiverId == receiverId)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToListAsync();

        public async Task<int> CountUnreadAsync(Guid receiverId) =>
            await _dbSet.CountAsync(n => n.ReceiverId == receiverId && !n.IsRead);
    }
}
