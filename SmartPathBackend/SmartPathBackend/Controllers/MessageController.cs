using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messages;
        public MessageController(IMessageService messages) => _messages = messages;

        [HttpGet("by-chat/{chatId:guid}")]
        public async Task<IActionResult> ByChat(Guid chatId) =>
            Ok(await _messages.GetMessagesByChatAsync(chatId));

        [HttpPost]
        public async Task<IActionResult> Send(MessageRequestDto req)
        {
            var senderId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var m = await _messages.SendMessageAsync(senderId, req);
            return Ok(m);
        }

        [HttpPut("{messageId:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid messageId) =>
            await _messages.MarkAsReadAsync(messageId) ? NoContent() : NotFound();
    }
}
