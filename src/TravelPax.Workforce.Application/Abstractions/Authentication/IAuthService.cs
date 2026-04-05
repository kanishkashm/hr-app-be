using TravelPax.Workforce.Contracts.Auth;

namespace TravelPax.Workforce.Application.Abstractions.Authentication;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
}
