using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class BotService : IBotService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public BotService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<BotConversationResponse> CreateConversationAsync(Guid ownerId, BotConversationCreateRequest req)
        {
            var now = DateTime.UtcNow;
            var convo = new BotConversation
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Title = string.IsNullOrWhiteSpace(req.Title) ? "New Chat" : req.Title!.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _uow.BotConversations.AddAsync(convo);

            // Nếu có SystemPrompt => tạo message hệ thống đầu tiên
            if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
            {
                var sysMsg = new BotMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = convo.Id,
                    SenderId = ownerId,             // gắn về chủ sở hữu (người học); phân biệt bằng Role
                    Role = BotMessageRole.System,
                    Content = req.SystemPrompt!.Trim(),
                    CreatedAt = now
                };
                await _uow.BotMessages.AddAsync(sysMsg);
            }

            await _uow.SaveChangesAsync();

            // map + đếm
            var resp = _mapper.Map<BotConversationResponse>(convo);
            resp.MessageCount = await _uow.BotMessages.CountByConversationAsync(convo.Id);
            return resp;
        }

        public async Task<(IReadOnlyList<BotConversationResponse> Items, int Total)> GetMyConversationsAsync(Guid ownerId, int page, int pageSize)
        {
            var total = await _uow.BotConversations.CountByOwnerAsync(ownerId);
            var list = await _uow.BotConversations.GetByOwnerAsync(ownerId, page, pageSize);
            var items = list.Select(c =>
            {
                var dto = _mapper.Map<BotConversationResponse>(c);
                dto.MessageCount = (c.Messages?.Count) ?? 0;
                return dto;
            }).ToList();
            return (items, total);
        }

        public async Task<BotConversationWithMessagesResponse?> GetConversationWithMessagesAsync(Guid ownerId, Guid conversationId, int limit = 50, Guid? beforeMessageId = null)
        {
            var convo = await _uow.BotConversations.GetWithMessagesAsync(conversationId, ownerId, limit, beforeMessageId);
            if (convo == null) return null;

            var dto = _mapper.Map<BotConversationWithMessagesResponse>(convo);
            dto.MessageCount = await _uow.BotMessages.CountByConversationAsync(conversationId);
            dto.Messages = (convo.Messages ?? Array.Empty<BotMessage>())
                .Select(_mapper.Map<BotMessageResponse>)
                .ToList();
            return dto;
        }

        public async Task<bool> RenameConversationAsync(Guid ownerId, Guid conversationId, string title)
        {
            var c = await _uow.BotConversations.GetByIdAsync(conversationId);
            if (c == null || c.OwnerId != ownerId) return false;
            c.Title = title.Trim();
            c.UpdatedAt = DateTime.UtcNow;
            _uow.BotConversations.Update(c);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteConversationAsync(Guid ownerId, Guid conversationId)
        {
            var c = await _uow.BotConversations.GetByIdAsync(conversationId);
            if (c == null || c.OwnerId != ownerId) return false;

            await _uow.BotMessages.DeleteByConversationAsync(conversationId);
            _uow.BotConversations.Remove(c);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<BotMessageResponse> AppendMessageAsync(Guid ownerId, BotMessageRequest req)
        {
            // quyền sở hữu
            var convo = await _uow.BotConversations.GetByIdAsync(req.ConversationId);
            if (convo == null || convo.OwnerId != ownerId)
                throw new UnauthorizedAccessException();

            var now = DateTime.UtcNow;
            var entity = new BotMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = req.ConversationId,
                SenderId = ownerId, // ràng về chủ sở hữu; phân biệt bot/user bằng Role
                Role = req.Role,
                Content = req.Content,
                CreatedAt = now,
                PromptTokens = req.PromptTokens,
                CompletionTokens = req.CompletionTokens,
                TotalTokens = req.TotalTokens,
                LatencyMs = req.LatencyMs,
                ToolCallsJson = req.ToolCallsJson
            };

            await _uow.BotMessages.AddAsync(entity);

            // bump UpdatedAt để session “nổi” lên
            convo.UpdatedAt = now;
            _uow.BotConversations.Update(convo);

            await _uow.SaveChangesAsync();
            return _mapper.Map<BotMessageResponse>(entity);
        }

        public async Task<IReadOnlyList<BotMessageResponse>> GetMessagesAsync(Guid ownerId, Guid conversationId, int limit = 50, Guid? beforeMessageId = null)
        {
            var convo = await _uow.BotConversations.GetByIdAsync(conversationId);
            if (convo == null || convo.OwnerId != ownerId) return Array.Empty<BotMessageResponse>();

            var list = await _uow.BotMessages.GetByConversationAsync(conversationId, limit, beforeMessageId);
            return list.Select(_mapper.Map<BotMessageResponse>).ToList();
        }

        public async Task<bool> DeleteMessageAsync(Guid ownerId, Guid messageId)
        {
            var msg = await _uow.BotMessages.GetByIdAsync(messageId);
            if (msg == null) return false;

            var convo = await _uow.BotConversations.GetByIdAsync(msg.ConversationId);
            if (convo == null || convo.OwnerId != ownerId) return false;

            _uow.BotMessages.Remove(msg);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
