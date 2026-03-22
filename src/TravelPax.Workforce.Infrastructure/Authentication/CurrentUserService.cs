using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;

namespace TravelPax.Workforce.Infrastructure.Authentication;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user?.FindFirstValue("nameid");
            return Guid.TryParse(raw, out var userId) ? userId : null;
        }
    }

    public string? Email
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Email)
                ?? user?.FindFirstValue(JwtRegisteredClaimNames.Email);
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
