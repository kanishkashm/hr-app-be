namespace TravelPax.Workforce.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TravelPax.Workforce";
    public string Audience { get; set; } = "TravelPax.Workforce.Web";
    public string SecretKey { get; set; } = "TravelPax-Workforce-Development-Key-2026-Change-Me";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}
