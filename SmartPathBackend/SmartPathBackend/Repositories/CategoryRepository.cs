using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using System;

namespace SmartPathBackend.Repositories
{
    public class CategoryRepository: BaseRepository<Category>, ICategoryRepository
    {
        private readonly SmartPathDbContext _ctx;
        public CategoryRepository(SmartPathDbContext ctx) : base(ctx) => _ctx = ctx;

        public IQueryable<Category> Query() => _ctx.Categories;

        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        {
            var q = _ctx.Categories.AsQueryable().Where(c => c.Name == name);
            if (excludeId.HasValue) q = q.Where(c => c.Id != excludeId.Value);
            return await q.AnyAsync(ct);
        }
    }
}
