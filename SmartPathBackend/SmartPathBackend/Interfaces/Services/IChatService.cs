using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IChatService
    {
        Task<Chat> StartChatAsync(Chat request);
        Task<IEnumerable<ChatResponseDto>> GetChatsByUserAsync(Guid userId);
        Task<ChatResponseDto?> GetByIdAsync(Guid chatId);
    }
}
