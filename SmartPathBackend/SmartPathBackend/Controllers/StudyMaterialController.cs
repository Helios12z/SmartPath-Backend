using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;
using SmartPathBackend.Utils;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudyMaterialController : ControllerBase
    {
        private readonly IStudyMaterialLibraryService _svc;
        public StudyMaterialController(IStudyMaterialLibraryService svc) => _svc = svc;

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] Guid? categoryId,
            [FromQuery] Status? status,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _svc.SearchAsync(categoryId, status ?? Status.Accepted, q, page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            Guid? requester = User.Identity?.IsAuthenticated == true ? User.GetUserIdOrThrow() : null;
            var item = await _svc.GetByIdAsync(requester, id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> Mine([FromQuery] Status? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var uid = User.GetUserIdOrThrow();
            var (items, total) = await _svc.GetMineAsync(uid, status, page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }

        [HttpPost]
        [Authorize]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Create([FromForm] StudyMaterialCreateMeta meta, IFormFile? file, CancellationToken ct)
        {
            var uid = User.GetUserIdOrThrow();
            var created = await _svc.CreateAsync(uid, meta, file, ct);
            return Ok(created);
        }

        [HttpPut("{id:guid}/review")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReview(Guid id, [FromBody] ReviewDecisionRequest req)
        {
            var adminId = User.GetUserIdOrThrow();
            var ok = await _svc.AdminReviewAsync(adminId, id, req);
            return ok ? NoContent() : NotFound();
        }
    }
}
