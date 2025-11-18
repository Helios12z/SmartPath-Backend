using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;
using System.Security.Claims;
using System.Diagnostics;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotService _svc;
        private readonly ILLMService _llm; 

        public BotController(IBotService svc, ILLMService llm) 
        {
            _svc = svc;
            _llm = llm;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("conversations")]
        public async Task<IActionResult> Create([FromBody] BotConversationCreateRequest req)
        {
            var uid = GetUserId();
            var resp = await _svc.CreateConversationAsync(uid, req);
            return Ok(resp);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> Mine([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var uid = GetUserId();
            var (items, total) = await _svc.GetMyConversationsAsync(uid, page, pageSize);
            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("conversations/{id:guid}")]
        public async Task<IActionResult> GetWithMessages([FromRoute] Guid id, [FromQuery] int limit = 50, [FromQuery] Guid? beforeMessageId = null)
        {
            var uid = GetUserId();
            var resp = await _svc.GetConversationWithMessagesAsync(uid, id, limit, beforeMessageId);
            return resp is null ? NotFound() : Ok(resp);
        }

        [HttpPatch("conversations/{id:guid}/title")]
        public async Task<IActionResult> Rename([FromRoute] Guid id, [FromBody] RenameConversationRequest req)
        {
            var uid = GetUserId();
            var ok = await _svc.RenameConversationAsync(uid, id, req.Title);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("conversations/{id:guid}")]
        public async Task<IActionResult> DeleteConversation([FromRoute] Guid id)
        {
            var uid = GetUserId();
            var ok = await _svc.DeleteConversationAsync(uid, id);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("messages")]
        public async Task<IActionResult> Append([FromBody] BotMessageRequest req)
        {
            var uid = GetUserId();
            var resp = await _svc.AppendMessageAsync(uid, req);
            return Ok(resp);
        }

        [HttpGet("conversations/{id:guid}/messages")]
        public async Task<IActionResult> ListMessages([FromRoute] Guid id, [FromQuery] int limit = 50, [FromQuery] Guid? beforeMessageId = null)
        {
            var uid = GetUserId();
            var resp = await _svc.GetMessagesAsync(uid, id, limit, beforeMessageId);
            return Ok(resp);
        }

        [HttpDelete("messages/{id:guid}")]
        public async Task<IActionResult> DeleteMessage([FromRoute] Guid id)
        {
            var uid = GetUserId();
            var ok = await _svc.DeleteMessageAsync(uid, id);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] BotGenerateRequest req, CancellationToken ct)
        {
            var uid = GetUserId();

            // 1. Lưu message của user
            var userMsg = await _svc.AppendMessageAsync(uid, new BotMessageRequest
            {
                ConversationId = req.ConversationId,
                Role = BotMessageRole.User,
                Content = req.UserContent
            });

            // 2. Lấy history để gửi lên LLM
            var limit = req.ContextLimit ?? 20;
            var ctx = await _svc.GetMessagesAsync(uid, req.ConversationId, limit);

            var forLlm = ctx.Select(m => (
                m.Role == BotMessageRole.Assistant ? "assistant" :
                m.Role == BotMessageRole.System ? "system" : "user",
                m.Content
            ));

            var (systemPrompt, contexts, sources) = await _svc.BuildRagSystemPromptAsync(
                ownerId: uid,
                conversationId: req.ConversationId,
                baseSystemPrompt: req.SystemPrompt,
                userContent: req.UserContent,
                topK: limit,
                ct: ct
            );

            // 4. Gọi LLM + đo latency
            var sw = Stopwatch.StartNew();

            var completion = await _llm.CompleteAsync(
                systemPrompt,
                forLlm,
                req.Model,
                ct
            );

            sw.Stop();

            // 5. Lưu message của assistant (có thể set LatencyMs luôn)
            var assistantMsg = await _svc.AppendMessageAsync(uid, new BotMessageRequest
            {
                ConversationId = req.ConversationId,
                Role = BotMessageRole.Assistant,
                Content = completion,
                // Nếu sau này có token usage thật thì bổ sung thêm vào đây
                LatencyMs = (int)sw.ElapsedMilliseconds
            });

            // 6. Build meta trả về cho FE (để FE show “Nguồn tham khảo” + link)
            var meta = new BotGenerateMeta
            {
                UsedModel = string.IsNullOrWhiteSpace(req.Model) ? null : req.Model,
                // Hiện tại chưa lấy được token từ LLM => để null / 0 tùy bạn
                PromptTokens = null,
                CompletionTokens = null,
                TotalTokens = null,
                LatencyMs = (int)sw.ElapsedMilliseconds,
                RetrievedContextCount = contexts?.Count ?? 0,
                Contexts = contexts,
                Sources = sources
            };

            return Ok(new BotGenerateResponse
            {
                UserMessage = userMsg,
                AssistantMessage = assistantMsg,
                Meta = meta
            });
        }
    }
}
