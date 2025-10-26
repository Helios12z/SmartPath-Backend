using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotService _svc;
        public BotController(IBotService svc) => _svc = svc;

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Conversations
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

        // Messages
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
    }
}
