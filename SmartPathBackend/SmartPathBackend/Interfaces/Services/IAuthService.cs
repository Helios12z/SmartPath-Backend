using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(string emailOrUsername, string password);
        Task<string?> RefreshAsync(string refreshToken);
    }
}
