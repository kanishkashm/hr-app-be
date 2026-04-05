using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.Authentication;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Contracts.Auth;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Authentication;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    TravelPaxDbContext dbContext,
    IJwtTokenGenerator tokenGenerator,
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = request.EmailOrUsername.Trim().ToUpperInvariant();
        var user = await userManager.Users
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(
                x => x.NormalizedEmail == normalized || x.NormalizedUserName == normalized,
                cancellationToken);

        if (user is null)
        {
            await WriteLoginAuditAsync(null, request.EmailOrUsername, "Failure", "User not found", cancellationToken);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!signInResult.Succeeded)
        {
            var reason = signInResult.IsLockedOut ? "Account locked." : "Invalid credentials.";
            await WriteLoginAuditAsync(user, request.EmailOrUsername, "Failure", reason, cancellationToken);
            throw new UnauthorizedAccessException(reason);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var permissions = await ResolvePermissionsAsync(user.Id, cancellationToken);
        var accessToken = tokenGenerator.GenerateAccessToken(user, roles, permissions);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedByIp = GetIpAddress()
        });

        await WriteLoginAuditAsync(user, request.EmailOrUsername, "Success", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            DateTimeOffset.UtcNow.AddMinutes(60),
            MapProfile(user, roles, permissions));
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existingToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = existingToken.User;
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var permissions = await ResolvePermissionsAsync(user.Id, cancellationToken);
        var newAccessToken = tokenGenerator.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = tokenGenerator.GenerateRefreshToken();

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken.Token;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            CreatedByIp = GetIpAddress()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            newAccessToken,
            newRefreshToken.Token,
            DateTimeOffset.UtcNow.AddMinutes(60),
            MapProfile(user, roles, permissions));
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await userManager.Users
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var permissions = await ResolvePermissionsAsync(user.Id, cancellationToken);

        return MapProfile(user, roles, permissions);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await userManager.Users
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        user.MustChangePassword = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = user.Id;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "PasswordChanged",
            Module = "Authentication",
            EntityName = nameof(AppUser),
            EntityId = user.Id.ToString(),
            NewValues = "Password changed by user"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken && x.RevokedAt == null, cancellationToken);

            if (token is not null)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
            }
        }

        if (currentUserService.UserId is { } userId)
        {
            var lastLogin = await dbContext.LoginAuditLogs
                .Where(x => x.UserId == userId && x.Status == "Success" && x.LogoutAt == null)
                .OrderByDescending(x => x.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastLogin is not null)
            {
                lastLogin.LogoutAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> ResolvePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rolePermissions = await (
            from userRole in dbContext.UserRoles
            join rolePermission in dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
            select permission.Name)
            .ToListAsync(cancellationToken);

        var directPermissions = await dbContext.UserPermissions
            .Where(x => x.UserId == userId && x.IsGranted)
            .Select(x => x.Permission.Name)
            .ToListAsync(cancellationToken);

        return rolePermissions
            .Concat(directPermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
    }

    private async Task WriteLoginAuditAsync(
        AppUser? user,
        string emailOrUsername,
        string status,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        dbContext.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserId = user?.Id,
            EmailOrUsername = emailOrUsername,
            Status = status,
            FailureReason = failureReason,
            IpAddress = GetIpAddress(),
            UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            DeviceSummary = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string GetIpAddress()
    {
        var forwardedFor = httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(forwardedFor)
            ? httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwardedFor;
    }

    private static UserProfileResponse MapProfile(AppUser user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        return new UserProfileResponse(
            user.Id,
            user.EmployeeId,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.Department ?? string.Empty,
            user.Designation ?? string.Empty,
            user.Branch?.Name ?? string.Empty,
            user.MustChangePassword,
            roles.ToArray(),
            permissions);
    }
}
