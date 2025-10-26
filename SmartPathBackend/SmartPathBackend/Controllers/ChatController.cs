using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Utils;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chats;
        public ChatController(IChatService chats) => _chats = chats;

        [HttpGet("mine")]
        public async Task<IActionResult> MyChats()
        {
            var userId = User.GetUserIdOrThrow();
            return Ok(await _chats.GetChatsByUserAsync(userId));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var me = User.GetUserIdOrThrow();
            var c = await _chats.GetByIdForUserAsync(me, id);
            return c is null ? NotFound() : Ok(c);
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] Chat req)
        {
            var chat = await _chats.StartChatAsync(req);
            return Ok(chat);
        }

        [HttpPost("direct/{otherUserId:guid}")]
        public async Task<IActionResult> GetOrCreateDirect(Guid otherUserId)
        {
            var me = User.GetUserIdOrThrow();
            var chat = await _chats.GetOrCreateDirectChatAsync(me, otherUserId);
            return Ok(chat);
        }
    }
}
