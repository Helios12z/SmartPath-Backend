namespace SmartPathBackend.Models.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? FullName { get; set; }
    }

    public class LoginRequest
    {
        public string EmailOrUsername { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
