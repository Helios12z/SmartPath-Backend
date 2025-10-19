using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
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
        public async Task<IActionResult> React(ReactionRequestDto req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var r = await _reactions.ReactAsync(userId, req);
            return Ok(r);
        }

        [HttpDelete("{postId:guid}")]
        public async Task<IActionResult> Remove(Guid postId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _reactions.RemoveReactionAsync(postId, userId) ? NoContent() : NotFound();
        }
    }
}
