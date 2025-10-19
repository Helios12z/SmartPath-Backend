namespace SmartPathBackend.Models.Options
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Base64Key { get; set; } = default!;
        public int AccessTokenMinutes { get; set; } = 120;
        public int RefreshTokenDays { get; set; } = 30;
    }
}
