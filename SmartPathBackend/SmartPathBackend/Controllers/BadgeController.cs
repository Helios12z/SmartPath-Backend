using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadgeController : ControllerBase
    {
        private readonly IBadgeService _badges;
        public BadgeController(IBadgeService badges) => _badges = badges;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var list = await _badges.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var badge = await _badges.GetByIdAsync(id);
            return badge is null ? NotFound() : Ok(badge);
        }

        [HttpGet("by-point/{point:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByPoint(int point)
        {
            var badge = await _badges.GetByPointAsync(point);
            return badge is null ? NotFound() : Ok(badge);
        }

        [HttpGet("by-name/{name}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByName(string name)
        {
            var badge = await _badges.GetByNameAsync(name);
            return badge is null ? NotFound() : Ok(badge);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] BadgeRequestDTO request)
        {
            try
            {
                var created = await _badges.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message, field = ex.ParamName });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BadgeRequestDTO request)
        {
            try
            {
                var updated = await _badges.UpdateAsync(id, request);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message, field = ex.ParamName });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _badges.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}
