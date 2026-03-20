using TravelPax.Workforce.Domain.Entities;

namespace TravelPax.Workforce.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(AppUser user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken();
}
