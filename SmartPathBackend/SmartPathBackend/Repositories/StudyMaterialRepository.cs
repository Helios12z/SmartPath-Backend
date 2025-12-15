using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Repositories
{
    public class StudyMaterialRepository : BaseRepository<StudyMaterial>, IStudyMaterialRepository
    {
        private readonly SmartPathDbContext _db;
        public StudyMaterialRepository(SmartPathDbContext db) : base(db) { 
            _db = db;
        }

        public Task<StudyMaterial?> GetWithCategoryAsync(Guid id) =>
            _db.StudyMaterials.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);

        public async Task<(List<StudyMaterial> Items, int Total)> SearchAsync(Guid? categoryId, Status? status, string? q, int page, int pageSize)
        {
            var query = _db.StudyMaterials.Include(x => x.Category).AsQueryable();

            if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.Title.ToLower().Contains(q.ToLower()));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }

        public async Task<(List<StudyMaterial> Items, int Total)> GetMineAsync(Guid uploaderId, Status? status, int page, int pageSize)
        {
            var query = _db.StudyMaterials.Include(x => x.Category).Where(x => x.UploaderId == uploaderId);
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }
    }
}
