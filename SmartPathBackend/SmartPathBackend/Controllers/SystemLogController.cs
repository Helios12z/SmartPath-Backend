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
    public class SystemLogController : ControllerBase
    {
        private readonly ISystemLogService _logs;
        public SystemLogController(ISystemLogService logs) => _logs = logs;

        [HttpGet("recent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Recent([FromQuery] int limit = 50) =>
            Ok(await _logs.GetRecentAsync(limit));

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> Mine()
        {
            var uid = User.GetUserIdOrThrow();
            return Ok(await _logs.GetByUserAsync(uid));
        }
    }
}
