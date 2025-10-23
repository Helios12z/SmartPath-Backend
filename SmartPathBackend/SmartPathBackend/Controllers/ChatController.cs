using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
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
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _chats.GetChatsByUserAsync(userId));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var c = await _chats.GetByIdAsync(id);
            return c is null ? NotFound() : Ok(c);
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] Chat req)
        {
            var chat = await _chats.StartChatAsync(req);
            return Ok(chat);
        }
    }
}
