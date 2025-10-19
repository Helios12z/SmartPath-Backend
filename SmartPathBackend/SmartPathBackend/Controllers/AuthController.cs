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

        public AuthController(IAuthService auth) { _auth = auth; }

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
    }
}
