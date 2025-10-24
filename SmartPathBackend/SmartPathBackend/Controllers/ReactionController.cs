using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Utils;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReactionController : ControllerBase
    {
        private readonly IReactionService _reactions;
        public ReactionController(IReactionService reactions) => _reactions = reactions;

        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> React([FromBody] ReactionRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var r = await _reactions.ReactAsync(userId, req);
            return Ok(r);
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Remove([FromBody] ReactionRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _reactions.RemoveReactionAsync(userId, req.PostId, req.CommentId);
            return ok ? NoContent() : NotFound();
        }
    }
}
