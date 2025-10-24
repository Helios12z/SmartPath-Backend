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
    public class PostController : ControllerBase
    {
        private readonly IPostService _posts;
        public PostController(IPostService posts) => _posts = posts;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var userId= User.GetUserIdOrNull();
            return Ok(await _posts.GetAllAsync(userId));
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
        public async Task<IActionResult> GetByUser(Guid userId) =>
            Ok(await _posts.GetByUserAsync(userId));

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(PostRequestDto req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var p = await _posts.CreateAsync(userId, req);
            return CreatedAtAction(nameof(GetById), new { id = p.Id }, p);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, PostRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var p = await _posts.UpdateAsync(id, req, userId);
            return p is null ? NotFound() : Ok(p);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id) =>
            await _posts.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
