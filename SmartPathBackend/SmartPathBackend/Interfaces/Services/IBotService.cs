using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IBotService
    {
        Task<BotConversationResponse> CreateConversationAsync(Guid ownerId, BotConversationCreateRequest req);
        Task<(IReadOnlyList<BotConversationResponse> Items, int Total)> GetMyConversationsAsync(Guid ownerId, int page, int pageSize);
        Task<BotConversationWithMessagesResponse?> GetConversationWithMessagesAsync(Guid ownerId, Guid conversationId, int limit = 50, Guid? beforeMessageId = null);

        Task<bool> RenameConversationAsync(Guid ownerId, Guid conversationId, string title);
        Task<bool> DeleteConversationAsync(Guid ownerId, Guid conversationId);

        Task<BotMessageResponse> AppendMessageAsync(Guid ownerId, BotMessageRequest req);
        Task<IReadOnlyList<BotMessageResponse>> GetMessagesAsync(Guid ownerId, Guid conversationId, int limit = 50, Guid? beforeMessageId = null);
        Task<bool> DeleteMessageAsync(Guid ownerId, Guid messageId);
    }
}
