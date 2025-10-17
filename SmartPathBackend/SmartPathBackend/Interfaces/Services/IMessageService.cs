using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IMessageService
    {
        Task<MessageResponseDto> SendMessageAsync(Guid senderId, MessageRequestDto request);
        Task<IEnumerable<MessageResponseDto>> GetMessagesByChatAsync(Guid chatId);
        Task<bool> MarkAsReadAsync(Guid messageId);
    }
}
