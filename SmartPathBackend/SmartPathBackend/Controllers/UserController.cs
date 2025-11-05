using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Utils;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _users;
        public UserController(IUserService users) => _users = users;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll() =>
            Ok(await _users.GetAllAsync());

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var u = await _users.GetByIdAsync(id);
            return u is null ? NotFound() : Ok(u);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(UserRequestDto req)
        {
            req.Role = Models.Enums.Role.Student;
            var u = await _users.CreateAsync(req);
            return u is null ? BadRequest() : CreatedAtAction(nameof(GetById), new { id = u.Id }, u);
        }

        [HttpPost("create-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdmin(UserRequestDto req)
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

        [HttpPut("{id:guid}/ban")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Ban(Guid id, [FromQuery] DateTime? until, [FromBody] string? reason)
        {
            var adminId = User.GetUserIdOrThrow();
            var ok = await _users.BanAsync(id, until, reason, adminId);
            return ok ? NoContent() : NotFound();
        }

        [HttpPut("{id:guid}/unban")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Unban(Guid id)
        {
            var adminId = User.GetUserIdOrThrow();
            var ok = await _users.UnbanAsync(id, adminId);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("{id:guid}/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserAdminSummaryDto>> Summary(Guid id)
        {
            var s = await _users.GetAdminSummaryAsync(id);
            return s is null ? NotFound() : Ok(s);
        }

        [HttpGet("analytics/users-created")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IReadOnlyList<DailyCountDto>>> UsersCreated([FromQuery] int days = 30)
            => Ok(await _users.GetUsersCreatedAsync(days));

        [HttpGet("analytics/activity")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IReadOnlyList<ActivityDailyDto>>> Activity([FromQuery] int days = 30)
            => Ok(await _users.GetActivityDailyAsync(days));

        [HttpGet("analytics/users-created-range")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UsersCreatedRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var data = await _users.GetUsersCreatedRangeAsync(start, end);
            return Ok(data ?? Array.Empty<DailyCountDto>());
        }

        [HttpGet("analytics/activity-range")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivityRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var data = await _users.GetActivityDailyRangeAsync(start, end);
            return Ok(data ?? Array.Empty<ActivityDailyDto>());
        }
    }
}
