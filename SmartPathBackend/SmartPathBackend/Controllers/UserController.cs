using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _users;
        public UserController(IUserService users) => _users = users;

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll() =>
            Ok(await _users.GetAllAsync());

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var u = await _users.GetByIdAsync(id);
            return u is null ? NotFound() : Ok(u);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create(UserRequestDto req)
        {
            var u = await _users.CreateAsync(req);
            return u is null ? BadRequest() : CreatedAtAction(nameof(GetById), new { id = u.Id }, u);
        }

        [HttpPut("{id:guid}")]
        [Authorize] 
        public async Task<IActionResult> Update(Guid id, UserRequestDto req)
        {
            var u = await _users.UpdateAsync(id, req);
            return u is null ? NotFound() : Ok(u);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id) =>
            await _users.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
