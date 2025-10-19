using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _noti;
        public NotificationController(INotificationService noti) => _noti = noti;

        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _noti.GetByReceiverAsync(uid));
        }

        [HttpGet("mine/unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _noti.CountUnreadAsync(uid));
        }

        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id) =>
            await _noti.MarkAsReadAsync(id) ? NoContent() : NotFound();
    }
}
