using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Utils;
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
        [Authorize]
        public async Task<IActionResult> Mine()
        {
            var uid = User.GetUserIdOrThrow();
            return Ok(await _noti.GetByReceiverAsync(uid));
        }

        [HttpGet("mine/unread-count")]
        [Authorize]
        public async Task<IActionResult> UnreadCount()
        {
            var uid = User.GetUserIdOrThrow();
            return Ok(await _noti.CountUnreadAsync(uid));
        }

        [HttpPut("{id:guid}/read")]
        [Authorize]
        public async Task<IActionResult> MarkRead(Guid id) =>
            await _noti.MarkAsReadAsync(id) ? NoContent() : NotFound();

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var uid = User.GetUserIdOrThrow();
            return await _noti.DeleteAsync(id, uid) ? NoContent() : NotFound();
        }
        
        [HttpDelete("mine/read")]
        [Authorize]
        public async Task<IActionResult> DeleteAllReadMine()
        {
            var uid = User.GetUserIdOrThrow();
            var deleted = await _noti.DeleteAllReadAsync(uid);
            return Ok(new { deleted });
        }
    }
}
