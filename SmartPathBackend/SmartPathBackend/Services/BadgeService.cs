using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly IUnitOfWork _uow;
        public BadgeService(IUnitOfWork uow) => _uow = uow;

        private static IQueryable<BadgeResponseDTO> ProjectToDto(IQueryable<Badge> q)
        {
            return q.Select(b => new BadgeResponseDTO
            {
                Id = b.Id,
                Name = b.Name,
                Point = b.Point
            });
        }

        public async Task<IEnumerable<BadgeResponseDTO>> GetAllAsync()
        {
            var q = _uow.Badges.Query().AsNoTracking();
            return await ProjectToDto(q.OrderBy(b => b.Point).ThenBy(b => b.Name))
                .ToListAsync();
        }

        public async Task<BadgeResponseDTO?> GetByIdAsync(Guid id)
        {
            var q = _uow.Badges.Query()
                    .AsNoTracking()
                    .Where(b => b.Id == id);
            return await ProjectToDto(q).FirstOrDefaultAsync();
        }

        public async Task<BadgeResponseDTO?> GetByPointAsync(int point)
        {
            var q = _uow.Badges.Query()
                    .AsNoTracking()
                    .Where(b => b.Point == point);
            return await ProjectToDto(q).FirstOrDefaultAsync();
        }

        public async Task<BadgeResponseDTO?> GetByNameAsync(string name)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ArgumentException("Name is required", nameof(name));

            var q = _uow.Badges.Query()
                    .AsNoTracking()
                    .Where(b => b.Name == trimmed);
            return await ProjectToDto(q).FirstOrDefaultAsync();
        }

        public async Task<BadgeResponseDTO> CreateAsync(BadgeRequestDTO request)
        {
            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required", nameof(request.Name));

            if (request.Point < 0)
                throw new ArgumentOutOfRangeException(nameof(request.Point), "Point must be non-negative.");

            if (await _uow.Badges.ExistsByNameOrPointAsync(name, request.Point))
                throw new InvalidOperationException("Badge name or point already exists.");

            var entity = new Badge
            {
                Id = Guid.NewGuid(),
                Name = name,
                Point = request.Point
            };

            await _uow.Badges.AddAsync(entity);
            await _uow.SaveChangesAsync();

            var q = _uow.Badges.Query()
                    .AsNoTracking()
                    .Where(b => b.Id == entity.Id);
            return await ProjectToDto(q).FirstAsync();
        }

        public async Task<BadgeResponseDTO?> UpdateAsync(Guid id, BadgeRequestDTO request)
        {
            var badge = await _uow.Badges.GetByIdAsync(id);
            if (badge == null) return null;

            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required", nameof(request.Name));

            if (request.Point < 0)
                throw new ArgumentOutOfRangeException(nameof(request.Point), "Point must be non-negative.");

            if (await _uow.Badges.ExistsByNameOrPointAsync(name, request.Point, excludeId: id))
                throw new InvalidOperationException("Badge name or point already exists.");

            badge.Name = name;
            badge.Point = request.Point;

            _uow.Badges.Update(badge);
            await _uow.SaveChangesAsync();

            var q = _uow.Badges.Query()
                    .AsNoTracking()
                    .Where(b => b.Id == id);
            return await ProjectToDto(q).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var badge = await _uow.Badges.GetByIdAsync(id);
            if (badge == null) return false;

            _uow.Badges.Remove(badge);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
