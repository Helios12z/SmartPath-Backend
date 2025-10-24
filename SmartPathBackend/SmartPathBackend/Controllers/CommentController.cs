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
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _comments;
        public CommentController(ICommentService comments) => _comments = comments;

        [HttpGet("by-post/{postId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByPost(Guid postId)
        {
            var userId = User.GetUserIdOrNull();
            return Ok(await _comments.GetByPostAsync(postId, userId));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CommentRequestDto req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var c = await _comments.CreateAsync(userId, req);
            return Ok(c);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, CommentRequestDto req)
        {
            var c = await _comments.UpdateAsync(id, req);
            return c is null ? NotFound() : Ok(c);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id) =>
            await _comments.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
