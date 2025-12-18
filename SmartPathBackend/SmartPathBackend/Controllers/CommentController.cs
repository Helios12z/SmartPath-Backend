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
        private readonly ISystemLogService _logs;

        public CommentController(ICommentService comments, ISystemLogService logs)
        {
            _comments = comments;
            _logs = logs;
        }

        [HttpGet("by-post/{postId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByPost(Guid postId)
        {
            var userId = User.GetUserIdOrNull();
            var items = await _comments.GetByPostAsync(postId, userId);

            return Ok(items);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CommentRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var c = await _comments.CreateAsync(userId, req);
            await _logs.CreateAsync(userId, "create", "comment", $"/api/Post/{req.PostId}");
            return Ok(c);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] CommentRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var c = await _comments.UpdateAsync(id, req);
            if (c is null) return NotFound();
            await _logs.CreateAsync(userId, "update", "comment", $"/api/Comment/{id}");
            return Ok(c);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _comments.DeleteAsync(id);
            if (!ok) return NotFound();
            await _logs.CreateAsync(userId, "delete", "comment", $"/api/Comment/{id}");
            return NoContent();
        }
    }
}
