using SmartPathBackend.Models.Entities;
using System.Linq.Expressions;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        Task<int> SaveChangesAsync();
    }
}
