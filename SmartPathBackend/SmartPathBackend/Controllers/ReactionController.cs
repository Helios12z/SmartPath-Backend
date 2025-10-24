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

        [HttpDelete("remove-post-reaction/{postId:guid}")]
        [Authorize]
        public async Task<IActionResult> RemovePostReaction(Guid postId)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _reactions.RemovePostReactionAsync(userId, postId);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("remove-comment-reaction/{commentId:guid}")]
        [Authorize]
        public async Task<IActionResult> RemoveCommentReaction(Guid commentId)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _reactions.RemoveCommentReactionAsync(userId, commentId);
            return ok ? NoContent() : NotFound();
        }
    }
}
