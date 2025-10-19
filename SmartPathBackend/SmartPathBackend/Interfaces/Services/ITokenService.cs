using System.Security.Claims;

namespace SmartPathBackend.Interfaces.Services
{
    public interface ITokenService
    {
        (string access, string refresh) CreatePair(Guid userId, string username, string role);
        string CreateAccess(Guid userId, string username, string role, string? jti = null);
        string CreateRefresh(Guid userId, string? jti = null);
        ClaimsPrincipal? Validate(string token, bool validateLifetime = true, bool expectRefresh = false);
    }
}
