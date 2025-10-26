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

            await _hub.Clients.Group($"chat-{dto.ChatId}")
                .SendAsync("NewMessage", dto);

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

            if (!msg.IsRead)
            {
                msg.IsRead = true;
                _unitOfWork.Messages.Update(msg);
                await _unitOfWork.SaveChangesAsync();

                var ev = new MessageReadEvent(messageId, msg.ChatId, readerId);
                await _hub.Clients.Group($"chat-{msg.ChatId}")
                    .SendAsync("MessageRead", ev);
            }
            return true;
        }
    }
}
