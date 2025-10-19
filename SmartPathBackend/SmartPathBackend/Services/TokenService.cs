using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartPathBackend.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _opt;
        private readonly SymmetricSecurityKey _key;
        private readonly TokenValidationParameters _baseParams;

        public TokenService(IOptions<JwtOptions> opt)
        {
            _opt = opt.Value;
            _key = new SymmetricSecurityKey(Convert.FromBase64String(_opt.Base64Key));
            _baseParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _opt.Issuer,
                ValidateAudience = true,
                ValidAudience = _opt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
        }

        public (string access, string refresh) CreatePair(Guid userId, string username, string role)
        {
            var jti = Guid.NewGuid().ToString("N");
            return (CreateAccess(userId, username, role, jti), CreateRefresh(userId, jti));
        }

        public string CreateAccess(Guid userId, string username, string role, string? jti = null)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti ?? Guid.NewGuid().ToString("N")),
                new Claim("type", "access"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefresh(Guid userId, string? jti = null)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jti ?? Guid.NewGuid().ToString("N")),
                new Claim("type", "refresh")
            };
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(_opt.RefreshTokenDays),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? Validate(string token, bool validateLifetime = true, bool expectRefresh = false)
        {
            var handler = new JwtSecurityTokenHandler();
            var p = _baseParams.Clone();
            p.ValidateLifetime = validateLifetime;
            try
            {
                var principal = handler.ValidateToken(token, p, out _);
                if (expectRefresh && principal.FindFirst("type")?.Value != "refresh") return null;
                return principal;
            }
            catch { return null; }
        }
    }
}
