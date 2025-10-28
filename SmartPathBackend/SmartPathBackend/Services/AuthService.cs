using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;
using SmartPathBackend.Utils;
using System.Security.Claims;

namespace SmartPathBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokens;
        private readonly IUserService _users;

        public AuthService(IUnitOfWork uow, ITokenService tokens, IUserService users)
        {
            _uow = uow; 
            _tokens = tokens;
            _users = users;
        }

        public async Task<AuthResponse?> LoginAsync(string emailOrUsername, string password)
        {
            var user = (await _uow.Users.FindAsync(u =>
                u.Email == emailOrUsername || u.Username == emailOrUsername)).FirstOrDefault();
            if (user is null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password)) return null;

            var pair = _tokens.CreatePair(user.Id, user.Username, user.Role.ToString());
            var response=new AuthResponse();
            response.AccessToken = pair.access;
            response.RefreshToken = pair.refresh;
            response.CurrentUserId=user.Id;
            return response; 
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

        public async Task<UserResponseDto> RegisterAsync(RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.FullName))
            {
                throw new ArgumentException("Missing required fields");
            }
            if (req.Password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters");
            }

            try
            {
                var created = await _users.CreateAsync(new UserRequestDto
                {
                    Email = req.Email,
                    Username = req.Username,
                    Password = req.Password,
                    FullName = req.FullName,
                    Role = req.Role   
                });

                if (created is null)
                    throw new InvalidOperationException("User creation failed.");

                return created;
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Email already exists", StringComparison.OrdinalIgnoreCase))
                    throw new DomainConflictException("user.email_exists", "Email already in use", "email");

                if (ex.Message.Contains("Username already exists", StringComparison.OrdinalIgnoreCase))
                    throw new DomainConflictException("user.username_exists", "Username already in use", "username");

                throw;
            }
        }
    }
}
