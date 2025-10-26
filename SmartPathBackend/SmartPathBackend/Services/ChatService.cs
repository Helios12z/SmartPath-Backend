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

        private static (Guid a, Guid b) NormalizePair(Guid userA, Guid userB)
        {
            return userA.CompareTo(userB) <= 0 ? (userA, userB) : (userB, userA);
        }

        public async Task<Chat> StartChatAsync(Chat request)
        {
            var (a, b) = NormalizePair(request.Member1Id, request.Member2Id);

            request.Member1Id = a;
            request.Member2Id = b;

            var existing = await _unitOfWork.Chats.GetDirectChatAsync(a, b);
            if (existing != null) return existing;

            request.Id = Guid.NewGuid();
            request.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Chats.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return request;
        }

        public async Task<IEnumerable<ChatResponseDto>> GetChatsByUserAsync(Guid userId)
        {
            var chats = await _unitOfWork.Chats.GetChatsByUserWithMessagesAsync(userId);

            var otherIds = chats
                .Select(c => c.Member1Id == userId ? c.Member2Id : c.Member1Id)
                .Distinct()
                .ToList();

            var users = await _unitOfWork.Users.GetByIdsAsync(otherIds); 
            var userDict = users.ToDictionary(u => u.Id);

            var ordered = chats.OrderByDescending(c => c.Messages.Any()
                ? c.Messages.Max(m => m.CreatedAt)
                : DateTime.MinValue);

            var dtos = ordered.Select(c =>
            {
                var dto = _mapper.Map<ChatResponseDto>(c);
                var otherId = c.Member1Id == userId ? c.Member2Id : c.Member1Id;
                if (userDict.TryGetValue(otherId, out var u))
                {
                    dto.OtherUser = new ChatOtherUserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        FullName = u.FullName,
                        AvatarUrl = u.AvatarUrl
                    };
                }
                return dto;
            });

            return dtos;
        }

        public async Task<ChatResponseDto?> GetByIdForUserAsync(Guid userId, Guid chatId)
        {
            var chat = await _unitOfWork.Chats.GetByIdWithMessagesAsync(chatId);
            if (chat is null) return null;

            var dto = _mapper.Map<ChatResponseDto>(chat);
            var otherId = chat.Member1Id == userId ? chat.Member2Id : chat.Member1Id;
            var u = await _unitOfWork.Users.GetByIdAsync(otherId);
            if (u != null)
            {
                dto.OtherUser = new ChatOtherUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName??"Unknown",
                    AvatarUrl = u.AvatarUrl
                };
            }
            return dto;
        }

        public async Task<ChatResponseDto> GetOrCreateDirectChatAsync(Guid userA, Guid userB)
        {
            if (userA == userB) throw new ArgumentException("Cannot start chat with yourself.");
            var (a, b) = NormalizePair(userA, userB);

            var existing = await _unitOfWork.Chats.GetDirectChatAsync(a, b);
            if (existing != null)
            {
                var dto = _mapper.Map<ChatResponseDto>(existing);
                var otherId = existing.Member1Id == userA ? existing.Member2Id : existing.Member1Id;
                var u = await _unitOfWork.Users.GetByIdAsync(otherId);
                if (u != null)
                {
                    dto.OtherUser = new ChatOtherUserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        FullName = u.FullName ?? "Unknown",
                        AvatarUrl = u.AvatarUrl
                    };
                }
                return dto;
            }

            var chat = new Chat { Id = Guid.NewGuid(), Name = null, Member1Id = a, Member2Id = b, CreatedAt = DateTime.UtcNow };
            await _unitOfWork.Chats.AddAsync(chat);
            await _unitOfWork.SaveChangesAsync();

            var createdDto = _mapper.Map<ChatResponseDto>(chat);
            var otherNewId = chat.Member1Id == userA ? chat.Member2Id : chat.Member1Id;
            var other = await _unitOfWork.Users.GetByIdAsync(otherNewId);
            if (other != null)
            {
                createdDto.OtherUser = new ChatOtherUserDto
                {
                    Id = other.Id,
                    Username = other.Username,
                    FullName = other.FullName ?? "Unknown",
                    AvatarUrl = other.AvatarUrl
                };
            }
            return createdDto;
        }
    }
}
