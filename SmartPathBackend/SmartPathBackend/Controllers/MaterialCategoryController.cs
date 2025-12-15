using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Utils;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialCategoryController : ControllerBase
    {
        private readonly IMaterialCategoryTreeService _svc;
        public MaterialCategoryController(IMaterialCategoryTreeService svc) => _svc = svc;

        [HttpGet("tree")]
        [AllowAnonymous]
        public async Task<IActionResult> Tree() => Ok(await _svc.GetTreeAsync());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] MaterialCategoryCreateRequest req)
        {
            var adminId = User.GetUserIdOrThrow();
            var node = await _svc.CreateAsync(adminId, req);
            return Ok(node);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MaterialCategoryUpdateRequest req)
        {
            var adminId = User.GetUserIdOrThrow();
            var node = await _svc.UpdateAsync(adminId, id, req);
            return node is null ? NotFound() : Ok(node);
        }

        [HttpPut("{id:guid}/move")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Move(Guid id, [FromBody] MoveCategoryRequest req)
        {
            var adminId = User.GetUserIdOrThrow();
            var ok = await _svc.MoveAsync(adminId, id, req);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var adminId = User.GetUserIdOrThrow();
            var ok = await _svc.DeleteAsync(adminId, id);
            return ok ? NoContent() : NotFound();
        }
    }
}
