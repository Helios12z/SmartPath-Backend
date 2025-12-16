using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IMessageService
    {
        Task<MessageResponseDto> SendMessageAsync(Guid senderId, MessageRequestDto request);
        Task<(IEnumerable<MessageResponseDto> Items, string? NextCursor)> GetMessagesByChatAsync(Guid chatId, string? cursor = null, int limit = 50);
        Task<bool> MarkAsReadAsync(Guid readerId, Guid messageId);
        Task MarkAllAsReadAsync(Guid readerId, Guid chatId);
    }
}
