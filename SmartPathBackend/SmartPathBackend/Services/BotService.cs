using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;
using System.Text;

namespace SmartPathBackend.Services
{
    public class BotService : IBotService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IEmbedderService _embedder;
        private readonly ILogger<BotService> _logger;

        const int MaxContextCharsTotal = 8000;   
        const int MaxChunkCharsEach = 1000;

        public BotService(IUnitOfWork uow, IMapper mapper, IEmbedderService embedder, ILogger<BotService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _embedder = embedder;
            _logger = logger;
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
                    SenderId = ownerId,             
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
            var convo = await _uow.BotConversations.GetByIdAsync(req.ConversationId);
            if (convo == null || convo.OwnerId != ownerId)
                throw new UnauthorizedAccessException();

            var now = DateTime.UtcNow;
            var entity = new BotMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = req.ConversationId,
                SenderId = ownerId,
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

            convo.UpdatedAt = now;
            await _uow.BotConversations.TouchUpdatedAtAsync(req.ConversationId, now);

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

        public async Task<(string SystemPrompt,
                        IReadOnlyList<RetrievedContextPreview> Contexts,
                        IReadOnlyList<KnowledgeSourcePreview> Sources)>
        BuildRagSystemPromptAsync(
            Guid ownerId,
            Guid conversationId,
            string? baseSystemPrompt,
            string userContent,
            int topK,
            CancellationToken ct = default)
        {
            var convo = await _uow.BotConversations.GetByIdAsync(conversationId);
            if (convo == null || convo.OwnerId != ownerId)
                throw new UnauthorizedAccessException();

            // 1) Embed câu hỏi của user
            var queryVec = await _embedder.EmbedOneAsync(userContent, ct);

            var k = topK > 0 ? topK : 6;

            // 2) Lấy các chunk gần nhất
            var chunks = await _uow.Knowledges.SearchByEmbeddingAsync(queryVec, k, ct);

            const string system = """
                Bạn là trợ lý AI cho ứng dụng Forum SmartPath phục vụ sinh viên UIT.

                Nguyên tắc bắt buộc khi trả lời:

                - CHỈ được sử dụng thông tin có trong phần "NGỮ CẢNH" do hệ thống cung cấp.
                - KHÔNG được tự bịa ví dụ, bảng, mã môn học (như IT001, EN001, ...) hoặc con số nếu NGỮ CẢNH không ghi rõ.
                - Không được tự suy luận thêm ý nghĩa mới cho mã môn học.
                  Ví dụ: nếu tài liệu nói "IT = nhóm ngành CNTT" thì không được gán IT cho "môn Toán".
                - Khi trích dẫn quy định/bảng/mã, phải giữ nguyên ký hiệu và ý nghĩa như trong tài liệu.
                - Nếu thông tin người dùng hỏi KHÔNG xuất hiện rõ trong ngữ cảnh, hãy trả lời:
                  "Trong tài liệu cung cấp không nêu rõ phần này, nên mình không chắc." và dừng lại, không đoán thêm.
                - Cố gắng trả lời đầy đủ thông tin (được cung cấp từ tài liệu) nhất có thể. 

                Luôn ưu tiên tính chính xác hơn là nói cho hay.
                """;

            // Không có ngữ cảnh => chỉ trả system prompt
            if (chunks.Count == 0)
                return (system,
                    Array.Empty<RetrievedContextPreview>(),
                    Array.Empty<KnowledgeSourcePreview>());

            var docScores = new Dictionary<Guid, double>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];

                var score = 1.0 / (1 + i);

                if (!docScores.TryAdd(chunk.DocumentId, score))
                    docScores[chunk.DocumentId] += score;
            }

            int maxSources = 3;

            var orderedDocs = docScores
                .OrderByDescending(kv => kv.Value)
                .ToList();

            var bestScore = orderedDocs[0].Value;
            double minScoreRatio = 0.5; // doc phải đạt >= 50% score doc tốt nhất

            var topDocIds = orderedDocs
                .Where(kv => kv.Value >= bestScore * minScoreRatio)
                .Take(maxSources)
                .Select(kv => kv.Key)
                .ToHashSet();

            // 3) Lấy danh sách document tương ứng để build Sources
            var docIds = chunks
                .Select(c => c.DocumentId)
                .Distinct()
                .ToList();

            var docsQuery = _uow.Knowledges.QueryDocuments()
                                           .Where(d => docIds.Contains(d.Id));

            var docs = await docsQuery.ToListAsync(ct);
            var docDict = docs.ToDictionary(d => d.Id);

            // 4) Build system prompt + context
            var sb = new StringBuilder();
            sb.AppendLine(system);
            sb.AppendLine();
            sb.AppendLine("NGỮ CẢNH:");

            int used = 0;
            var contextPreviews = new List<RetrievedContextPreview>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var cut = chunk.Content;
                if (cut.Length > MaxChunkCharsEach)
                    cut = cut[..MaxChunkCharsEach];

                if (used + cut.Length > MaxContextCharsTotal)
                    break;

                sb.AppendLine($"--- [{i + 1}] ---");

                // Gắn info tài liệu ngay trước đoạn text (để model thấy được nguồn)
                if (docDict.TryGetValue(chunk.DocumentId, out var doc))
                {
                    if (!string.IsNullOrWhiteSpace(doc.Title) ||
                        !string.IsNullOrWhiteSpace(doc.SourceUrl))
                    {
                        sb.AppendLine(
                            $"(Từ tài liệu: {doc.Title ?? "Không tiêu đề"} - {doc.SourceUrl})");
                    }
                }

                sb.AppendLine(cut);
                used += cut.Length;

                // 5) Thêm preview cho Meta.Contexts
                var snippet = cut.Length > 200 ? cut[..200] + "..." : cut;
                contextPreviews.Add(new RetrievedContextPreview
                {
                    ChunkId = chunk.Id,
                    DocumentId = chunk.DocumentId,
                    ChunkIndex = chunk.ChunkIndex,
                    Snippet = snippet
                });
            }

            // 6) Build danh sách nguồn cho Meta.Sources (unique theo document)
            var sources = docs
                .Where(d => topDocIds.Contains(d.Id))   
                .Select(d => new KnowledgeSourcePreview
                {
                    DocumentId = d.Id,
                    Title = d.Title,
                    SourceUrl = d.SourceUrl
                })
                .ToList();

            var fullPrompt = sb.ToString();

            // LOG ra toàn bộ system prompt + NGỮ CẢNH được gửi cho LLM
            //_logger.LogInformation(
            //    "RAG prompt for conversation {ConversationId}, userContent = {UserContent}\n{Prompt}",
            //    conversationId,
            //    userContent,
            //    fullPrompt
            //);

            return (sb.ToString(), contextPreviews, sources);
        }
    }
}
