using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Utils;

namespace SmartPathBackend.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHubContext<MessageHub> _hub;

        public MessageService(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<MessageHub> hub)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _hub = hub;
        }

        public async Task<MessageResponseDto> SendMessageAsync(Guid senderId, MessageRequestDto request)
        {
            // Get chat to verify user is part of it
            var chat = await _unitOfWork.Chats.GetByIdAsync(request.ChatId);
            if (chat == null || (chat.Member1Id != senderId && chat.Member2Id != senderId))
            {
                throw new UnauthorizedAccessException("User is not a member of this chat");
            }

            var msg = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = request.ChatId,
                SenderId = senderId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.Messages.AddAsync(msg);
            await _unitOfWork.SaveChangesAsync();

            var sender = await _unitOfWork.Users.GetByIdAsync(senderId);

            var dto = new MessageResponseDto
            {
                Id = msg.Id,
                ChatId = msg.ChatId,
                Content = msg.Content,
                SenderId = msg.SenderId,
                SenderUsername = sender?.Username ?? "unknown",
                IsRead = msg.IsRead,
                CreatedAt = msg.CreatedAt
            };

            // Send to chat group for real-time updates
            await _hub.Clients.Group($"chat-{dto.ChatId}")
                .SendAsync("NewMessage", dto);

            // Also send notification to the recipient's user group
            var recipientId = chat.Member1Id == senderId ? chat.Member2Id : chat.Member1Id;
            await _hub.Clients.Group($"user-{recipientId}")
                .SendAsync("NewMessageNotification", new {
                    ChatId = chat.Id,
                    MessageId = msg.Id,
                    SenderUsername = sender?.Username ?? "unknown",
                    Content = msg.Content.Length > 50 ? msg.Content.Substring(0, 50) + "..." : msg.Content,
                    CreatedAt = msg.CreatedAt
                });

            return dto;
        }

        public async Task<IEnumerable<MessageResponseDto>> GetMessagesByChatAsync(Guid chatId)
        {
            var messages = await _unitOfWork.Messages.GetMessagesByChatAsync(chatId);

            var result = new List<MessageResponseDto>();
            foreach (var m in messages)
            {
                var sender = m.Sender ?? await _unitOfWork.Users.GetByIdAsync(m.SenderId);

                result.Add(new MessageResponseDto
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    SenderUsername = sender?.Username ?? "unknown",
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt
                });
            }
            return result;
        }

        public async Task<bool> MarkAsReadAsync(Guid readerId, Guid messageId)
        {
            var msg = await _unitOfWork.Messages.GetByIdAsync(messageId);
            if (msg == null) return false;

            // Don't allow sender to mark their own message as read
            if (msg.SenderId == readerId) return false;

            if (!msg.IsRead)
            {
                msg.IsRead = true;
                _unitOfWork.Messages.Update(msg);
                await _unitOfWork.SaveChangesAsync();

                // Send read receipt to sender
                await _hub.Clients.Group($"user-{msg.SenderId}")
                    .SendAsync("MessageRead", new {
                        MessageId = msg.Id,
                        ChatId = msg.ChatId,
                        ReaderId = readerId,
                        ReadAt = DateTime.UtcNow
                    });

                // Also send to chat group for UI updates
                await _hub.Clients.Group($"chat-{msg.ChatId}")
                    .SendAsync("MessageStatusUpdated", new {
                        MessageId = msg.Id,
                        IsRead = true,
                        ReaderId = readerId
                    });
            }
            return true;
        }

        public async Task MarkAllAsReadAsync(Guid readerId, Guid chatId)
        {
            // Get unread messages for this user in this chat
            var messages = await _unitOfWork.Messages.GetUnreadMessagesAsync(readerId, chatId);

            if (!messages.Any()) return;

            // Track senders to notify them
            var sendersToNotify = new HashSet<Guid>();

            foreach (var message in messages)
            {
                if (!message.IsRead && message.SenderId != readerId)
                {
                    message.IsRead = true;
                    _unitOfWork.Messages.Update(message);
                    sendersToNotify.Add(message.SenderId);

                    // Send update to chat group
                    await _hub.Clients.Group($"chat-{chatId}")
                        .SendAsync("MessageStatusUpdated", new {
                            MessageId = message.Id,
                            IsRead = true,
                            ReaderId = readerId
                        });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Notify all senders that their messages were read
            foreach (var senderId in sendersToNotify)
            {
                await _hub.Clients.Group($"user-{senderId}")
                    .SendAsync("MessagesReadInChat", new {
                        ChatId = chatId,
                        ReaderId = readerId,
                        ReadAt = DateTime.UtcNow
                    });
            }
        }
    }
}
