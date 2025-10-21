using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IUserService _userService;

        public AuthController(IAuthService auth, IUserService userService) 
        { 
            _auth = auth; 
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var tokens = await _auth.LoginAsync(req.EmailOrUsername, req.Password);
            if (tokens is null) return Unauthorized("Invalid credentials");
            return Ok(new { accessToken = tokens.Value.access, refreshToken = tokens.Value.refresh });
        }

        public record RefreshDto(string RefreshToken);

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
        {
            var access = await _auth.RefreshAsync(dto.RefreshToken);
            return access is null ? Unauthorized("Invalid refresh token") : Ok(new { accessToken = access });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.FullName))
            {
                return BadRequest("Missing required fields");
            }

            var created = await _userService.CreateAsync(new UserRequestDto
            {
                Email = req.Email,
                Username = req.Username,
                Password = req.Password,
                FullName = req.FullName
            });

            if (created == null) return BadRequest("Failed to create user");

            return Ok(new
            {
                message = "Registration successful. Please log in to continue.",
                user = created
            });
        }
    }
}
