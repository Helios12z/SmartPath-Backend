using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;
using System.Security.Claims;

namespace SmartPathBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokens;

        public AuthService(IUnitOfWork uow, ITokenService tokens)
        {
            _uow = uow; _tokens = tokens;
        }

        public async Task<(string access, string refresh)?> LoginAsync(string emailOrUsername, string password)
        {
            var user = (await _uow.Users.FindAsync(u =>
                u.Email == emailOrUsername || u.Username == emailOrUsername)).FirstOrDefault();
            if (user is null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password)) return null;

            var pair = _tokens.CreatePair(user.Id, user.Username, user.Role.ToString());
            return pair;
        }

        public async Task<string?> RefreshAsync(string refreshToken)
        {
            var principal = _tokens.Validate(refreshToken, validateLifetime: true, expectRefresh: true);
            if (principal is null) return null;

            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(sub, out var userId)) return null;

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user is null) return null;

            return _tokens.CreateAccess(user.Id, user.Username, user.Role.ToString());
        }
    }
}
