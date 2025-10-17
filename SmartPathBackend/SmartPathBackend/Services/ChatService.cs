using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ChatService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Chat> StartChatAsync(Chat request)
        {
            var existing = await _unitOfWork.Chats.GetDirectChatAsync(request.Member1Id, request.Member2Id);
            if (existing != null) return existing;

            request.Id = Guid.NewGuid();
            request.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Chats.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return request;
        }

        public async Task<IEnumerable<ChatResponseDto>> GetChatsByUserAsync(Guid userId)
        {
            var chats = await _unitOfWork.Chats.GetChatsByUserAsync(userId);
            return _mapper.Map<IEnumerable<ChatResponseDto>>(chats);
        }

        public async Task<ChatResponseDto?> GetByIdAsync(Guid chatId)
        {
            var chat = await _unitOfWork.Chats.GetByIdAsync(chatId);
            return chat == null ? null : _mapper.Map<ChatResponseDto>(chat);
        }
    }
}
