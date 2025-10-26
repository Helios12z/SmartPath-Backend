using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;
using SmartPathBackend.Utils;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly ISystemLogService _logs;

        public ReportController(IReportService reports, ISystemLogService logs)
        {
            _reports = reports;
            _logs = logs;
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending() => Ok(await _reports.GetPendingAsync());

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.GetUserIdOrThrow();
            return Ok(await _reports.GetByReporterAsync(userId));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ReportRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var r = await _reports.CreateAsync(userId, req);
            await _logs.CreateAsync(userId, "create", "report", $"/api/Report/{r.Id}");
            return Ok(r);
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] Status status)
        {
            var adminId = User.GetUserIdOrThrow();
            var ok = await _reports.UpdateStatusAsync(id, status);
            if (!ok) return NotFound();
            await _logs.CreateAsync(adminId, "update", "report", $"/api/Report/{id}");
            return NoContent();
        }
    }
}
