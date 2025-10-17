using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByReceiverAsync(Guid receiverId);
        Task<int> CountUnreadAsync(Guid receiverId);
    }
}
