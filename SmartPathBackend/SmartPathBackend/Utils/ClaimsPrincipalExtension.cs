using System.Security.Claims;

namespace SmartPathBackend.Utils
{
    public static class ClaimsPrincipalExtension
    {
        public static Guid GetUserIdOrThrow(this ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue(ClaimTypes.Name);
            if (!Guid.TryParse(id, out var guid))
                throw new UnauthorizedAccessException("Invalid or missing user id claim.");
            return guid;
        }

        public static Guid? GetUserIdOrNull(this ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue(ClaimTypes.Name);
            return Guid.TryParse(id, out var guid) ? guid : (Guid?)null;
        }
    }
}
