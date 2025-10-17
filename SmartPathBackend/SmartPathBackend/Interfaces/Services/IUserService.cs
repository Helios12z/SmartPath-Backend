using SmartPathBackend.Models.DTOs;

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
    }
}
