using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface ISystemLogService
    {
        Task<IEnumerable<SystemLogResponseDto>> GetRecentAsync(int limit = 50);
        Task<IEnumerable<SystemLogResponseDto>> GetByUserAsync(Guid userId);
        Task CreateAsync(Guid? userId, string action, string targetType, string? url = null);
    }
}
