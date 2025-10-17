using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MessageService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            return _mapper.Map<MessageResponseDto>(msg);
        }

        public async Task<IEnumerable<MessageResponseDto>> GetMessagesByChatAsync(Guid chatId)
        {
            var messages = await _unitOfWork.Messages.GetMessagesByChatAsync(chatId);
            return _mapper.Map<IEnumerable<MessageResponseDto>>(messages);
        }

        public async Task<bool> MarkAsReadAsync(Guid messageId)
        {
            var msg = await _unitOfWork.Messages.GetByIdAsync(messageId);
            if (msg == null) return false;

            msg.IsRead = true;
            _unitOfWork.Messages.Update(msg);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
