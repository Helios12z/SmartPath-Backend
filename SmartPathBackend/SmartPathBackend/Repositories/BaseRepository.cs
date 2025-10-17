using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using System.Linq.Expressions;

namespace SmartPathBackend.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        protected readonly SmartPathDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(SmartPathDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet.AsNoTracking().ToListAsync();

        public virtual async Task<T?> GetByIdAsync(Guid id) =>
            await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual void Update(T entity) => _dbSet.Update(entity);

        public virtual void Remove(T entity) => _dbSet.Remove(entity);

        public virtual async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
