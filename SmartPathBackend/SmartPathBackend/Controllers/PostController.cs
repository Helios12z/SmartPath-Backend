using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Utils;
using System.Security.Claims;
using static System.Reflection.Metadata.BlobBuilder;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPostService _posts;
        private readonly ISystemLogService _logs; 
        public PostController(IPostService posts, ISystemLogService logs)
        {
            _posts = posts;
            _logs = logs;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.GetUserIdOrNull();
            var items = await _posts.GetAllAsync(userId);

            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.GetUserIdOrNull();
            var p = await _posts.GetByIdAsync(id, userId);
            return p is null ? NotFound() : Ok(p);
        }

        [HttpGet("by-user/{userId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var items = await _posts.GetByUserAsync(userId);

            return Ok(items);
        }

        [HttpGet("recommendations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendations()
        {
            var userId = User.GetUserIdOrNull();
            var recommendations = await _posts.GetRecommendationsAsync(userId);
            return Ok(recommendations);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] PostRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var p = await _posts.CreateAsync(userId, req);
            await _logs.CreateAsync(userId, "create", "post", $"/api/Post/{p.Id}");
            return Ok(p);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] PostRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var p = await _posts.UpdateAsync(id, req, userId);
            if (p is null) return NotFound();
            await _logs.CreateAsync(userId, "update", "post", $"/api/Post/{id}");
            return Ok(p);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserIdOrThrow();
            var ok = await _posts.DeleteAsync(id);
            if (!ok) return NotFound();
            await _logs.CreateAsync(userId, "delete", "post", $"/api/Post/{id}");
            return NoContent();
        }
    }
}
