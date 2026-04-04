using System.Security.Cryptography;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TravelPax.Workforce.Application.Abstractions.Notifications;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Users;
using TravelPax.Workforce.Contracts.Users;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Users;

public sealed class UserService(
    TravelPaxDbContext dbContext,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ICurrentUserService currentUserService,
    IEmailOutboxService emailOutboxService,
    IConfiguration configuration) : IUserService
{
    public async Task<UserListResponse> GetUsersAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.Include(x => x.Branch).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.NormalizedEmail!.Contains(term) ||
                x.EmployeeId.ToUpper().Contains(term) ||
                x.DisplayName.ToUpper().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(x => x.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserListItemResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new UserListItemResponse(
                user.Id,
                user.EmployeeId,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.Department ?? string.Empty,
                user.Designation ?? string.Empty,
                user.EmploymentType ?? string.Empty,
                user.Branch?.Name ?? string.Empty,
                user.Status,
                user.LastLoginAt,
                roles.ToArray()));
        }

        return new UserListResponse(items, totalCount);
    }

    public async Task<UserDetailResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(x => x.Branch)
            .Include(x => x.ReportingManager)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        return await MapDetailAsync(user);
    }

    public async Task<UserDetailResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var temporaryPassword = GenerateTemporaryPassword();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = request.Email.Trim().ToUpperInvariant(),
            NormalizedUserName = request.Email.Trim().ToUpperInvariant(),
            MobileNumber = request.MobileNumber,
            Department = request.Department,
            Designation = request.Designation,
            EmploymentType = request.EmploymentType,
            DateJoined = request.DateJoined,
            ReportingManagerId = request.ReportingManagerId,
            BranchId = request.BranchId,
            Status = request.Status,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedBy = actorId
        };

        var result = await userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await ApplyRolesAsync(user, request.RoleCodes);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = "UserCreated",
            Module = "Users",
            EntityName = nameof(AppUser),
            EntityId = user.Id.ToString(),
            NewValues = $"Email={user.Email};Status={user.Status};Roles={string.Join(',', request.RoleCodes)}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var loginUrl = ResolveLoginUrl();
            await emailOutboxService.QueueAsync(
                [user.Email],
                "TravelPax Workforce: Your account is ready",
                BuildNewUserOnboardingText(user.DisplayName, user.Email, temporaryPassword, loginUrl),
                BuildNewUserOnboardingHtml(user.DisplayName, user.Email, temporaryPassword, loginUrl),
                actorId,
                cancellationToken);
        }

        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task<UserDetailResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var oldValues = $"Email={user.Email};Status={user.Status};Department={user.Department};Designation={user.Designation}";

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = request.Email.Trim();
        user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
        user.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
        user.MobileNumber = request.MobileNumber;
        user.Department = request.Department;
        user.Designation = request.Designation;
        user.EmploymentType = request.EmploymentType;
        user.DateJoined = request.DateJoined;
        user.ReportingManagerId = request.ReportingManagerId;
        user.BranchId = request.BranchId;
        user.Status = request.Status;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = currentUserService.UserId;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(x => x.Description)));
        }

        await ReplaceRolesAsync(user, request.RoleCodes);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserService.UserId,
            Action = "UserUpdated",
            Module = "Users",
            EntityName = nameof(AppUser),
            EntityId = user.Id.ToString(),
            OldValues = oldValues,
            NewValues = $"Email={user.Email};Status={user.Status};Department={user.Department};Designation={user.Designation};Roles={string.Join(',', request.RoleCodes)}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task<UserDetailResponse> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        user.Status = request.Status;
        user.LockoutEnd = request.Status == "Inactive" ? DateTimeOffset.MaxValue : null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = currentUserService.UserId;

        await userManager.UpdateAsync(user);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserService.UserId,
            Action = "UserStatusUpdated",
            Module = "Users",
            EntityName = nameof(AppUser),
            EntityId = user.Id.ToString(),
            NewValues = $"Status={user.Status}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserService.UserId,
            Action = "UserPasswordReset",
            Module = "Users",
            EntityName = nameof(AppUser),
            EntityId = user.Id.ToString(),
            NewValues = "Password reset by admin"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDetailResponse> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return await GetUserAsync(currentUserService.UserId.Value, cancellationToken);
    }

    public async Task<UserDetailResponse> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await dbContext.Users
            .Include(x => x.Branch)
            .Include(x => x.ReportingManager)
            .FirstAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);
        user.DisplayName = request.DisplayName.Trim();
        user.MobileNumber = request.MobileNumber;
        user.EmergencyContactName = NormalizeNullable(request.EmergencyContactName);
        user.EmergencyContactPhone = NormalizeNullable(request.EmergencyContactPhone);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = user.Id;

        await userManager.UpdateAsync(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapDetailAsync(user);
    }

    public async Task<ProfileUpdateRequestResponse> SubmitMyProfileUpdateRequestAsync(
        CreateMyProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await dbContext.Users
            .FirstAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        var pendingExists = await dbContext.UserProfileUpdateRequests.AnyAsync(
            x => x.UserId == user.Id && x.Status == "Pending",
            cancellationToken);
        if (pendingExists)
        {
            throw new InvalidOperationException("You already have a pending profile update request.");
        }

        var entity = new UserProfileUpdateRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            CurrentDisplayName = user.DisplayName,
            CurrentMobileNumber = user.MobileNumber,
            CurrentEmergencyContactName = user.EmergencyContactName,
            CurrentEmergencyContactPhone = user.EmergencyContactPhone,
            RequestedDisplayName = request.DisplayName.Trim(),
            RequestedMobileNumber = NormalizeNullable(request.MobileNumber),
            RequestedEmergencyContactName = NormalizeNullable(request.EmergencyContactName),
            RequestedEmergencyContactPhone = NormalizeNullable(request.EmergencyContactPhone),
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedBy = user.Id,
            UpdatedBy = user.Id
        };

        dbContext.UserProfileUpdateRequests.Add(entity);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "ProfileUpdateRequested",
            Module = "Users",
            EntityName = nameof(UserProfileUpdateRequest),
            EntityId = entity.Id.ToString(),
            NewValues = $"DisplayName={entity.RequestedDisplayName};Mobile={entity.RequestedMobileNumber};EmergencyName={entity.RequestedEmergencyContactName};EmergencyPhone={entity.RequestedEmergencyContactPhone};Reason={entity.Reason}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapProfileUpdateRequest(entity);
    }

    public async Task<IReadOnlyCollection<ProfileUpdateRequestResponse>> GetMyProfileUpdateRequestsAsync(int take, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var items = await dbContext.UserProfileUpdateRequests
            .Include(x => x.User)
            .Include(x => x.ReviewedByUser)
            .Where(x => x.UserId == currentUserService.UserId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return items.Select(MapProfileUpdateRequest).ToArray();
    }

    public async Task<ProfileUpdateRequestListResponse> GetProfileUpdateRequestsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserProfileUpdateRequests
            .Include(x => x.User)
            .Include(x => x.ReviewedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status.Trim());
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ProfileUpdateRequestListResponse(items.Select(MapProfileUpdateRequest).ToArray(), total);
    }

    public async Task<ProfileUpdateRequestResponse> ReviewProfileUpdateRequestAsync(
        Guid requestId,
        ReviewProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var reviewerId = currentUserService.UserId.Value;
        var entity = await dbContext.UserProfileUpdateRequests
            .Include(x => x.User)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Profile update request not found.");

        if (entity.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending requests can be reviewed.");
        }

        entity.Status = request.Approve ? "Approved" : "Rejected";
        entity.ReviewedByUserId = reviewerId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewerComment = NormalizeNullable(request.ReviewerComment);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = reviewerId;

        if (request.Approve)
        {
            entity.User.DisplayName = entity.RequestedDisplayName;
            entity.User.MobileNumber = entity.RequestedMobileNumber;
            entity.User.EmergencyContactName = entity.RequestedEmergencyContactName;
            entity.User.EmergencyContactPhone = entity.RequestedEmergencyContactPhone;
            entity.User.UpdatedAt = DateTimeOffset.UtcNow;
            entity.User.UpdatedBy = reviewerId;
            entity.AppliedAt = DateTimeOffset.UtcNow;
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = reviewerId,
            Action = request.Approve ? "ProfileUpdateApproved" : "ProfileUpdateRejected",
            Module = "Users",
            EntityName = nameof(UserProfileUpdateRequest),
            EntityId = entity.Id.ToString(),
            NewValues = $"Status={entity.Status};Comment={entity.ReviewerComment};AppliedAt={entity.AppliedAt}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.ReviewedByUser).LoadAsync(cancellationToken);
        return MapProfileUpdateRequest(entity);
    }

    public async Task<IReadOnlyCollection<RoleOptionResponse>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await roleManager.Roles
            .OrderBy(x => x.Name)
            .Select(x => new RoleOptionResponse(x.Code, x.Name!))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BranchOptionResponse>> GetBranchOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.OfficeBranches
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new BranchOptionResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task<UserDetailResponse> MapDetailAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDetailResponse(
            user.Id,
            user.EmployeeId,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.MobileNumber,
            user.Department,
            user.Designation,
            user.EmploymentType,
            user.DateJoined,
            user.ReportingManagerId,
            user.ReportingManager?.DisplayName,
            user.BranchId,
            user.Branch?.Name,
            user.EmergencyContactName,
            user.EmergencyContactPhone,
            user.Status,
            user.LastLoginAt,
            roles.ToArray());
    }

    private static ProfileUpdateRequestResponse MapProfileUpdateRequest(UserProfileUpdateRequest item)
    {
        return new ProfileUpdateRequestResponse(
            item.Id,
            item.UserId,
            item.User.DisplayName,
            item.User.EmployeeId,
            item.CurrentDisplayName,
            item.CurrentMobileNumber,
            item.CurrentEmergencyContactName,
            item.CurrentEmergencyContactPhone,
            item.RequestedDisplayName,
            item.RequestedMobileNumber,
            item.RequestedEmergencyContactName,
            item.RequestedEmergencyContactPhone,
            item.Reason,
            item.Status,
            item.ReviewedByUserId,
            item.ReviewedByUser?.DisplayName,
            item.ReviewedAt,
            item.ReviewerComment,
            item.CreatedAt,
            item.AppliedAt);
    }

    private async Task ApplyRolesAsync(AppUser user, IReadOnlyCollection<string> roleCodes)
    {
        var validRoles = await roleManager.Roles
            .Where(x => roleCodes.Contains(x.Code))
            .Select(x => x.Name!)
            .ToListAsync();

        if (validRoles.Count > 0)
        {
            await userManager.AddToRolesAsync(user, validRoles);
        }
    }

    private async Task ReplaceRolesAsync(AppUser user, IReadOnlyCollection<string> roleCodes)
    {
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await ApplyRolesAsync(user, roleCodes);
    }

    private string ResolveLoginUrl()
    {
        var configuredUrl = configuration["App:FrontendBaseUrl"]
            ?? configuration["Frontend:BaseUrl"]
            ?? configuration["FrontendBaseUrl"];

        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return "TravelPax Workforce portal";
        }

        return configuredUrl.TrimEnd('/');
    }

    private static string GenerateTemporaryPassword(int length = 12)
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "@#$%&*!";
        var all = uppercase + lowercase + digits + symbols;

        var buffer = new List<char>(length)
        {
            uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)],
            lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)],
        };

        while (buffer.Count < length)
        {
            buffer.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        for (var i = buffer.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        return new string(buffer.ToArray());
    }

    private static string BuildNewUserOnboardingText(string displayName, string email, string temporaryPassword, string loginUrl)
    {
        return $@"Hello {displayName},

Your TravelPax Workforce account has been created.

Username: {email}
Temporary Password: {temporaryPassword}
Login URL: {loginUrl}

Please keep this password secure. If you have trouble signing in, contact your HR administrator.";
    }

    private static string BuildNewUserOnboardingHtml(string displayName, string email, string temporaryPassword, string loginUrl)
    {
        var encodedName = WebUtility.HtmlEncode(displayName);
        var encodedEmail = WebUtility.HtmlEncode(email);
        var encodedPassword = WebUtility.HtmlEncode(temporaryPassword);
        var encodedLoginUrl = WebUtility.HtmlEncode(loginUrl);
        var loginUrlHtml = loginUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || loginUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? $"""<a href="{encodedLoginUrl}">{encodedLoginUrl}</a>"""
            : encodedLoginUrl;

        return $@"<html>
  <body style=""font-family:Segoe UI,Arial,sans-serif;color:#1f2937;line-height:1.55;"">
    <h2 style=""margin:0 0 12px 0;color:#b8141a;"">Welcome to TravelPax Workforce</h2>
    <p style=""margin:0 0 12px 0;"">Hello <strong>{encodedName}</strong>, your account is now ready.</p>
    <table cellpadding=""8"" cellspacing=""0"" style=""border-collapse:collapse;margin:12px 0;border:1px solid #e5e7eb;"">
      <tr>
        <td style=""background:#f9fafb;border:1px solid #e5e7eb;""><strong>Username</strong></td>
        <td style=""border:1px solid #e5e7eb;"">{encodedEmail}</td>
      </tr>
      <tr>
        <td style=""background:#f9fafb;border:1px solid #e5e7eb;""><strong>Temporary Password</strong></td>
        <td style=""border:1px solid #e5e7eb;""><code style=""font-size:14px;"">{encodedPassword}</code></td>
      </tr>
      <tr>
        <td style=""background:#f9fafb;border:1px solid #e5e7eb;""><strong>Login URL</strong></td>
        <td style=""border:1px solid #e5e7eb;"">{loginUrlHtml}</td>
      </tr>
    </table>
    <p style=""margin:12px 0 0 0;"">Please keep this password secure. If you have trouble signing in, contact your HR administrator.</p>
  </body>
</html>";
    }

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
