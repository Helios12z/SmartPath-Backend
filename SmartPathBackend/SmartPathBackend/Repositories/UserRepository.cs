using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(SmartPathDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
            => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetByUsernameAsync(string username)
            => await _dbSet.FirstOrDefaultAsync(u => u.Username == username);

        public async Task<IEnumerable<User>> SearchAsync(string keyword)
            => await _dbSet.Where(u =>
                u.Username.Contains(keyword) ||
                u.Email.Contains(keyword) ||
                (u.FullName != null && u.FullName.Contains(keyword))
            ).ToListAsync();

        public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            if (ids is null) return Enumerable.Empty<User>();

            var set = ids as ISet<Guid> ?? new HashSet<Guid>(ids);
            if (set.Count == 0) return Enumerable.Empty<User>();

            return await _dbSet
                .AsNoTracking()
                .Where(u => set.Contains(u.Id))
                .ToListAsync();
        }
    }
}
