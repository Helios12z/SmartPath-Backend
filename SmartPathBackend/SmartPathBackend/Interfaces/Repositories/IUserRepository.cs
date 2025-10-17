using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> SearchAsync(string keyword);
    }
}
