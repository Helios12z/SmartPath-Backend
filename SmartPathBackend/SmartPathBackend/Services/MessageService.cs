using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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

        public async Task<(IEnumerable<MessageResponseDto> Items, string? NextCursor)> GetMessagesByChatAsync(Guid chatId, string? cursor = null, int limit = 50)
        {
            // Ensure valid limit
            limit = Math.Min(Math.Max(1, limit), 100); // Max 100 messages per request

            IOrderedQueryable<Message> query = _unitOfWork.Messages.Query()
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt);

            // Parse cursor (expects base64 encoded timestamp)
            DateTime? cursorTime = null;
            if (!string.IsNullOrEmpty(cursor))
            {
                try
                {
                    var cursorBytes = Convert.FromBase64String(cursor);
                    cursorTime = DateTime.FromBinary(BitConverter.ToInt64(cursorBytes, 0));
                }
                catch
                {
                    // Invalid cursor, ignore
                }
            }

            if (cursorTime.HasValue)
            {
                query = (IOrderedQueryable<Message>)query.Where(m => m.CreatedAt < cursorTime.Value);
            }

            var messages = await query
                .Take(limit + 1) // Take one extra to check if there are more
                .ToListAsync();

            // Check if there are more messages
            var hasMore = messages.Count > limit;
            if (hasMore)
            {
                messages.RemoveAt(messages.Count - 1); // Remove the extra item
            }

            // Generate next cursor from the last message
            string? nextCursor = null;
            if (hasMore && messages.Any())
            {
                var lastMessageTime = messages.Last().CreatedAt;
                var cursorBytes = BitConverter.GetBytes(lastMessageTime.ToBinary());
                nextCursor = Convert.ToBase64String(cursorBytes);
            }

            // Map to DTOs and reverse order to show oldest first
            var result = messages
                .AsEnumerable()
                .Reverse()
                .Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender?.Username ?? "unknown",
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt
                })
                .ToList();

            return (result, nextCursor);
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
