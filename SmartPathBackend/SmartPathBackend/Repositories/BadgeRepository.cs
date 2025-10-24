using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class BadgeRepository: BaseRepository<Badge>, IBadgeRepository
    {
        private readonly SmartPathDbContext _ctx;
        public BadgeRepository(SmartPathDbContext ctx) : base(ctx) => _ctx = ctx;

        public IQueryable<Badge> Query() => _ctx.Badges;

        public async Task<bool> ExistsByNameOrPointAsync(
            string name,
            int point,
            Guid? excludeId = null,
            CancellationToken ct = default)
        {
            var q = _ctx.Badges.AsNoTracking()
                .Where(b => b.Name == name || b.Point == point);

            if (excludeId.HasValue)
                q = q.Where(b => b.Id != excludeId.Value);

            return await q.AnyAsync(ct);
        }
    }
}
