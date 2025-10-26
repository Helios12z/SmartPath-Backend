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
        private readonly ISystemLogService _logs;

        public ReactionController(IReactionService reactions, ISystemLogService logs)
        {
            _reactions = reactions;
            _logs = logs;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> React([FromBody] ReactionRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var r = await _reactions.ReactAsync(userId, req);
            await _logs.CreateAsync(userId, "create", "reaction", null);
            return Ok(r);
        }

        [HttpDelete("remove-post-reaction/{postId:guid}")]
        [Authorize]
        public async Task<IActionResult> RemovePostReaction(Guid postId)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _reactions.RemovePostReactionAsync(userId, postId);
            if (!ok) return NotFound();
            await _logs.CreateAsync(userId, "delete", "reaction", $"/api/Post/{postId}");
            return NoContent();
        }

        [HttpDelete("remove-comment-reaction/{commentId:guid}")]
        [Authorize]
        public async Task<IActionResult> RemoveCommentReaction(Guid commentId)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _reactions.RemoveCommentReactionAsync(userId, commentId);
            if (!ok) return NotFound();
            await _logs.CreateAsync(userId, "delete", "reaction", $"/api/Comment/{commentId}");
            return NoContent();
        }
    }
}
