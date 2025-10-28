using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Options;
using SmartPathBackend.Services;
using SmartPathBackend.Utils;
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
            var result = await _auth.LoginAsync(req.EmailOrUsername, req.Password);
            if (result is null) return Unauthorized("Invalid credentials");
            return Ok(new { accessToken = result.AccessToken, refreshToken = result.RefreshToken, currentUserId=result.CurrentUserId });
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
            try
            {
                var created = await _auth.RegisterAsync(req);

                return Ok(new
                {
                    message = "Registration successful. Please log in to continue.",
                    user = created
                });
            }
            catch (DomainConflictException ex)
            {
                return Conflict(new ApiError(ex.Code, ex.Message, ex.Field));
            }
            catch (ArgumentException ex)
            {
                var isPwd = ex.Message.Contains("Password") && ex.Message.Contains("6");
                var code = isPwd ? "validation.password_short" : "validation.error";
                var field = isPwd ? "password" : null;

                return BadRequest(new ApiError(code, ex.Message, field));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiError("user.create_failed", ex.Message));
            }
            catch
            {
                return StatusCode(500, new ApiError("server.error", "Unexpected server error"));
            }
        }
    }

    public record ApiError(string code, string message, string? field = null);
}
