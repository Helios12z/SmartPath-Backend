using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categories;
        public CategoryController(ICategoryService categories) => _categories = categories;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll() => Ok(await _categories.GetAllAsync());

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var c = await _categories.GetByIdAsync(id);
            return c is null ? NotFound() : Ok(c);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Create(CategoryRequestDto req)
        {
            var c = await _categories.CreateAsync(req);
            return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, CategoryRequestDto req)
        {
            var c = await _categories.UpdateAsync(id, req);
            return c is null ? NotFound() : Ok(c);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id) =>
            await _categories.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
