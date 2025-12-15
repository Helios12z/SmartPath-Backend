using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class MaterialCategoryTreeService : IMaterialCategoryTreeService
    {
        private readonly SmartPathDbContext _context;
        private readonly ILogger<MaterialCategoryTreeService> _logger;

        public MaterialCategoryTreeService(SmartPathDbContext context, ILogger<MaterialCategoryTreeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<MaterialCategoryNodeDto>> GetTreeAsync()
        {
            var categories = await _context.MaterialCategories
                .OrderBy(c => c.Level)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var categoryMap = categories.ToDictionary(c => c.Id, c => new MaterialCategoryNodeDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Path,
                c.Level,
                c.SortOrder,
                c.IsActive,
                new List<MaterialCategoryNodeDto>()
            ));

            var rootCategories = new List<MaterialCategoryNodeDto>();

            foreach (var category in categories)
            {
                if (category.ParentId.HasValue && categoryMap.ContainsKey(category.ParentId.Value))
                {
                    categoryMap[category.ParentId.Value].Children.Add(categoryMap[category.Id]);
                }
                else
                {
                    rootCategories.Add(categoryMap[category.Id]);
                }
            }

            return rootCategories;
        }

        public async Task<MaterialCategoryNodeDto> CreateAsync(Guid adminId, MaterialCategoryCreateRequest req)
        {
            // Validate slug uniqueness
            if (await _context.MaterialCategories.AnyAsync(c => c.Slug == req.Slug))
            {
                throw new ArgumentException($"Slug '{req.Slug}' already exists");
            }

            var category = new MaterialCategory
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                Slug = req.Slug,
                ParentId = req.ParentId,
                SortOrder = req.SortOrder,
                Level = req.ParentId.HasValue ? await GetCategoryLevelAsync(req.ParentId.Value) + 1 : 0,
                Path = await GenerateCategoryPathAsync(req.ParentId, req.Slug),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminId
            };

            _context.MaterialCategories.Add(category);
            await _context.SaveChangesAsync();

            return new MaterialCategoryNodeDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Path,
                category.Level,
                category.SortOrder,
                category.IsActive,
                new List<MaterialCategoryNodeDto>()
            );
        }

        public async Task<MaterialCategoryNodeDto?> UpdateAsync(Guid adminId, Guid id, MaterialCategoryUpdateRequest req)
        {
            var category = await _context.MaterialCategories.FindAsync(id);
            if (category == null)
                return null;

            // Validate slug uniqueness if changed
            if (category.Slug != req.Slug && await _context.MaterialCategories.AnyAsync(c => c.Slug == req.Slug && c.Id != id))
            {
                throw new ArgumentException($"Slug '{req.Slug}' already exists");
            }

            var oldParentId = category.ParentId;
            var oldSlug = category.Slug;

            category.Name = req.Name;
            category.Slug = req.Slug;
            category.ParentId = req.ParentId;
            category.SortOrder = req.SortOrder;
            category.IsActive = req.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = adminId;

            // Recalculate level and path if parent changed
            if (oldParentId != req.ParentId || oldSlug != req.Slug)
            {
                category.Level = req.ParentId.HasValue ? await GetCategoryLevelAsync(req.ParentId.Value) + 1 : 0;
                category.Path = await GenerateCategoryPathAsync(req.ParentId, req.Slug);

                // Update paths of all children
                await UpdateChildrenPathsAsync(category.Id, category.Path);
            }

            await _context.SaveChangesAsync();

            var children = await GetChildrenAsync(category.Id);

            return new MaterialCategoryNodeDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Path,
                category.Level,
                category.SortOrder,
                category.IsActive,
                children
            );
        }

        public async Task<bool> DeleteAsync(Guid adminId, Guid id)
        {
            var category = await _context.MaterialCategories.FindAsync(id);
            if (category == null)
                return false;

            // Check if category has children
            var hasChildren = await _context.MaterialCategories.AnyAsync(c => c.ParentId == id);
            if (hasChildren)
            {
                throw new ArgumentException("Cannot delete category with children. Delete or move children first.");
            }

            // Check if category has materials
            var hasMaterials = await _context.StudyMaterials.AnyAsync(m => m.CategoryId == id);
            if (hasMaterials)
            {
                throw new ArgumentException("Cannot delete category with materials. Move materials to another category first.");
            }

            _context.MaterialCategories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MoveAsync(Guid adminId, Guid id, MoveCategoryRequest req)
        {
            var category = await _context.MaterialCategories.FindAsync(id);
            if (category == null)
                return false;

            // Validate no circular reference
            if (req.NewParentId.HasValue)
            {
                if (req.NewParentId.Value == id)
                {
                    throw new ArgumentException("Cannot move category under itself");
                }

                var isDescendant = await IsDescendantAsync(req.NewParentId.Value, id);
                if (isDescendant)
                {
                    throw new ArgumentException("Cannot move category under its own descendant");
                }
            }

            category.ParentId = req.NewParentId;
            category.SortOrder = req.NewSortOrder;
            category.Level = req.NewParentId.HasValue ? await GetCategoryLevelAsync(req.NewParentId.Value) + 1 : 0;
            category.Path = await GenerateCategoryPathAsync(req.NewParentId, category.Slug);
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = adminId;

            // Update paths of all children
            await UpdateChildrenPathsAsync(category.Id, category.Path);

            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<int> GetCategoryLevelAsync(Guid parentId)
        {
            var parent = await _context.MaterialCategories.FindAsync(parentId);
            return parent?.Level + 1 ?? 0;
        }

        private async Task<string> GenerateCategoryPathAsync(Guid? parentId, string slug)
        {
            if (!parentId.HasValue)
                return $"/{slug}";

            var parent = await _context.MaterialCategories.FindAsync(parentId.Value);
            return parent != null ? $"{parent.Path}/{slug}" : $"/{slug}";
        }

        private async Task UpdateChildrenPathsAsync(Guid parentId, string parentPath)
        {
            var children = await _context.MaterialCategories
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                child.Path = $"{parentPath}/{child.Slug}";
                child.Level = await GetCategoryLevelAsync(parentId) + 1;

                // Recursively update grandchildren
                await UpdateChildrenPathsAsync(child.Id, child.Path);
            }
        }

        private async Task<List<MaterialCategoryNodeDto>> GetChildrenAsync(Guid parentId)
        {
            var children = await _context.MaterialCategories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var result = new List<MaterialCategoryNodeDto>();

            foreach (var child in children)
            {
                var grandChildren = await GetChildrenAsync(child.Id);
                result.Add(new MaterialCategoryNodeDto(
                    child.Id,
                    child.Name,
                    child.Slug,
                    child.Path,
                    child.Level,
                    child.SortOrder,
                    child.IsActive,
                    grandChildren
                ));
            }

            return result;
        }

        private async Task<bool> IsDescendantAsync(Guid categoryId, Guid ancestorId)
        {
            var category = await _context.MaterialCategories.FindAsync(categoryId);
            if (category == null || !category.ParentId.HasValue)
                return false;

            if (category.ParentId.Value == ancestorId)
                return true;

            return await IsDescendantAsync(category.ParentId.Value, ancestorId);
        }
    }
}