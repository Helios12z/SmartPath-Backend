using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponseDto>> GetByReceiverAsync(Guid receiverId);
        Task<int> CountUnreadAsync(Guid receiverId);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task CreateAsync(Guid receiverId, string type, string content, string? url = null);
    }
}
