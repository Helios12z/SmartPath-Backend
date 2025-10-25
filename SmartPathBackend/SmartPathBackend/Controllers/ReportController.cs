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
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reports;
        public ReportController(IReportService reports) => _reports = reports;

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
        public async Task<IActionResult> Create(ReportRequestDto req)
        {
            var userId = User.GetUserIdOrThrow();
            var r = await _reports.CreateAsync(userId, req);
            return Ok(r);
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status) =>
            await _reports.UpdateStatusAsync(id, status) ? NoContent() : NotFound();
    }
}
