using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class MaterialCategoryRepository : BaseRepository<MaterialCategory>, IMaterialCategoryRepository
    {
        private readonly SmartPathDbContext _db;
        public MaterialCategoryRepository(SmartPathDbContext db) : base(db) {
            _db = db;
        }

        public Task<List<MaterialCategory>> GetAllActiveAsync() =>
            _db.MaterialCategories.Where(x => x.IsActive).OrderBy(x => x.Path).ThenBy(x => x.SortOrder).ToListAsync();

        public Task<MaterialCategory?> GetBySlugAsync(string slug) =>
            _db.MaterialCategories.FirstOrDefaultAsync(x => x.Slug == slug);

        public async Task<List<MaterialCategory>> GetTreeAsync()
        {
            return await _db.MaterialCategories
                .Where(x => x.IsActive)
                .AsNoTracking()
                .OrderBy(x => x.Level).ThenBy(x => x.SortOrder)
                .ToListAsync();
        }
    }
}
