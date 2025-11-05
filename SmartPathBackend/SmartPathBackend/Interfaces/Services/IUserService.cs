using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllAsync();
        Task<UserResponseDto?> GetByIdAsync(Guid id);
        Task<UserResponseDto?> GetByEmailAsync(string email);
        Task<UserResponseDto?> CreateAsync(UserRequestDto request);
        Task<UserResponseDto?> UpdateAsync(Guid id, UserRequestDto request);
        Task<bool> DeleteAsync(Guid id);
        Task<User?> AuthenticateAsync(string emailOrUsername, string password);

        //For admin
        Task<bool> BanAsync(Guid id, DateTime? until, string? reason, Guid adminId);
        Task<bool> UnbanAsync(Guid id, Guid adminId);
        Task<UserAdminSummaryDto?> GetAdminSummaryAsync(Guid id);
        Task<IReadOnlyList<DailyCountDto>> GetUsersCreatedAsync(int days);
        Task<IReadOnlyList<ActivityDailyDto>> GetActivityDailyAsync(int days);
        Task<IReadOnlyList<DailyCountDto>> GetUsersCreatedRangeAsync(DateTime start, DateTime end);
        Task<IReadOnlyList<ActivityDailyDto>> GetActivityDailyRangeAsync(DateTime start, DateTime end);
    }
}
