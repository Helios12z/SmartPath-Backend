using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetByReceiverAsync(Guid receiverId)
        {
            var noti = await _unitOfWork.Notifications.GetByReceiverAsync(receiverId);
            return _mapper.Map<IEnumerable<NotificationResponseDto>>(noti);
        }

        public async Task<int> CountUnreadAsync(Guid receiverId)
            => await _unitOfWork.Notifications.CountUnreadAsync(receiverId);

        public async Task<bool> MarkAsReadAsync(Guid notificationId)
        {
            var noti = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (noti == null) return false;

            noti.IsRead = true;
            _unitOfWork.Notifications.Update(noti);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task CreateAsync(Guid receiverId, string type, string content, string? url = null)
        {
            var noti = new Notification
            {
                Id = Guid.NewGuid(),
                ReceiverId = receiverId,
                Type = type,
                Content = content,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(noti);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id, Guid receiverId)
        {
            var noti = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (noti == null || noti.ReceiverId != receiverId) return false;

            _unitOfWork.Notifications.Remove(noti);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        
        public async Task<int> DeleteAllReadAsync(Guid receiverId)
        {
            var affected = await _unitOfWork.Notifications.DeleteAllReadForReceiverAsync(receiverId);
            return affected;
        }
    }
}
