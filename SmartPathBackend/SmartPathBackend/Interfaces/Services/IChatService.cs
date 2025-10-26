using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IChatService
    {
        Task<IEnumerable<ChatResponseDto>> GetChatsByUserAsync(Guid userId);
        Task<ChatResponseDto?> GetByIdForUserAsync(Guid userId, Guid chatId); 
        Task<ChatResponseDto> GetOrCreateDirectChatAsync(Guid userA, Guid userB);
        Task<Chat> StartChatAsync(Chat request);
    }
}
