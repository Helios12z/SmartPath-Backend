using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartPathBackend.Interfaces;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SmartPathBackend.Utils
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServiceProvider _serviceProvider;

        public MessageHub(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _serviceProvider = serviceProvider;
        }

        public override async Task OnConnectedAsync()
        {
            var uidClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uidClaim))
            {
                Context.Abort();
                return;
            }

            var userId = Guid.Parse(uidClaim);
            Console.WriteLine($"[HUB] Connected: {Context.ConnectionId} user={userId}");

            // Auto-join all user's chats
            try
            {
                var userChats = await _unitOfWork.Chats.GetChatsByUserAsync(userId);
                foreach (var chat in userChats)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chat.Id}");
                    Console.WriteLine($"[HUB] User {userId} auto-joined chat {chat.Id}");
                }

                // Join user-specific group for notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HUB] Error auto-joining chats for user {userId}: {ex.Message}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[HUB] Disconnected: {Context.ConnectionId} err={exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> JoinChat(string chatId)
        {
            try
            {
                var uidClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(uidClaim))
                    return false;

                var userId = Guid.Parse(uidClaim);
                var chatGuid = Guid.Parse(chatId);

                // Validate user is part of this chat
                var chat = await _unitOfWork.Chats.GetByIdAsync(chatGuid);
                if (chat == null || (chat.Member1Id != userId && chat.Member2Id != userId))
                {
                    Console.WriteLine($"[HUB] User {userId} attempted to join unauthorized chat {chatId}");
                    return false;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");
                Console.WriteLine($"[HUB] User {userId} manually joined chat {chatId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HUB] Error joining chat {chatId}: {ex.Message}");
                return false;
            }
        }

        public Task LeaveChat(string chatId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{chatId}");

        public async Task MarkMessagesRead(string chatId)
        {
            try
            {
                var uidClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(uidClaim))
                    return;

                var userId = Guid.Parse(uidClaim);
                var chatGuid = Guid.Parse(chatId);

                // Get unread messages for this user in this chat
                var messages = await _unitOfWork.Messages.GetUnreadMessagesAsync(userId, chatGuid);

                foreach (var message in messages)
                {
                    if (!message.IsRead && message.SenderId != userId)
                    {
                        message.IsRead = true;
                        _unitOfWork.Messages.Update(message);

                        // Notify sender that message was read
                        await Clients.Group($"user-{message.SenderId}")
                            .SendAsync("MessageRead", new { MessageId = message.Id, ReaderId = userId });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HUB] Error marking messages read in chat {chatId}: {ex.Message}");
            }
        }

        public Task<string> Ping() => Task.FromResult("pong");
    }
}
