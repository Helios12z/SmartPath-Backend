using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using System;

namespace SmartPathBackend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _uow;
        public CategoryService(IUnitOfWork uow) => _uow = uow;

        private static IQueryable<CategoryResponseDto> ProjectToDto(IQueryable<Category> q)
        {
            return q.Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                PostCount = c.CategoryPosts != null ? c.CategoryPosts.Count() : 0
            });
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            var q = _uow.Categories.Query()
                        .AsNoTracking()
                        .Include(c => c.CategoryPosts);
            return await ProjectToDto(q.OrderBy(c => c.Name)).ToListAsync();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
        {
            var q = _uow.Categories.Query()
                        .AsNoTracking()
                        .Where(c => c.Id == id)
                        .Include(c => c.CategoryPosts);
            return await ProjectToDto(q).FirstOrDefaultAsync();
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryRequestDto request)
        {
            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required", nameof(request.Name));

            if (await _uow.Categories.ExistsByNameAsync(name))
                throw new InvalidOperationException("Category name already exists.");

            var entity = new Category { Id = Guid.NewGuid(), Name = name };
            await _uow.Categories.AddAsync(entity);
            await _uow.SaveChangesAsync();

            var q = _uow.Categories.Query()
                        .AsNoTracking()
                        .Where(c => c.Id == entity.Id)
                        .Include(c => c.CategoryPosts);
            return await ProjectToDto(q).FirstAsync();
        }

        public async Task<CategoryResponseDto?> UpdateAsync(Guid id, CategoryRequestDto request)
        {
            var cat = await _uow.Categories.GetByIdAsync(id);
            if (cat == null) return null;

            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required", nameof(request.Name));

            if (await _uow.Categories.ExistsByNameAsync(name, excludeId: id))
                throw new InvalidOperationException("Category name already exists.");

            cat.Name = name;
            _uow.Categories.Update(cat);
            await _uow.SaveChangesAsync();

            var q = _uow.Categories.Query()
                        .AsNoTracking()
                        .Where(c => c.Id == id)
                        .Include(c => c.CategoryPosts);
            return await ProjectToDto(q).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var cat = await _uow.Categories.GetByIdAsync(id);
            if (cat == null) return false;

            _uow.Categories.Remove(cat);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
