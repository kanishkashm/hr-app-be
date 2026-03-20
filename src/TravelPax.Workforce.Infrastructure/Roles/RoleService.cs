using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Roles;
using TravelPax.Workforce.Contracts.Roles;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Roles;

public sealed class RoleService(
    TravelPaxDbContext dbContext,
    RoleManager<AppRole> roleManager,
    ICurrentUserService currentUserService) : IRoleService
{
    public async Task<RoleListResponse> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new RoleListResponse(roles.Select(MapRole).ToArray());
    }

    public async Task<RoleResponse> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        return MapRole(role);
    }

    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Roles.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new InvalidOperationException("Role code already exists.");
        }

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            NormalizedName = request.Name.Trim().ToUpperInvariant(),
            Description = request.Description,
            IsSystem = false
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await ReplacePermissionsAsync(role, request.PermissionNames, cancellationToken);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserService.UserId,
            Action = "RoleCreated",
            Module = "Roles",
            EntityName = nameof(AppRole),
            EntityId = role.Id.ToString(),
            NewValues = $"Code={role.Code};Permissions={string.Join(',', request.PermissionNames)}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRoleAsync(role.Id, cancellationToken);
    }

    public async Task<RoleResponse> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var oldPermissions = await dbContext.RolePermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.Permission.Name)
            .ToListAsync(cancellationToken);

        role.Name = request.Name.Trim();
        role.NormalizedName = request.Name.Trim().ToUpperInvariant();
        role.Description = request.Description;
        role.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        await ReplacePermissionsAsync(role, request.PermissionNames, cancellationToken);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserService.UserId,
            Action = "RoleUpdated",
            Module = "Roles",
            EntityName = nameof(AppRole),
            EntityId = role.Id.ToString(),
            OldValues = $"Permissions={string.Join(',', oldPermissions)}",
            NewValues = $"Permissions={string.Join(',', request.PermissionNames)}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRoleAsync(role.Id, cancellationToken);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        if (role.IsSystem)
        {
            throw new InvalidOperationException("System roles cannot be deleted.");
        }

        var hasUsers = await dbContext.UserRoles.AnyAsync(x => x.RoleId == roleId, cancellationToken);
        if (hasUsers)
        {
            throw new InvalidOperationException("Cannot delete a role that is assigned to users.");
        }

        var permissions = await dbContext.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(permissions);
        await roleManager.DeleteAsync(role);

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserService.UserId,
            Action = "RoleDeleted",
            Module = "Roles",
            EntityName = nameof(AppRole),
            EntityId = role.Id.ToString(),
            OldValues = $"Code={role.Code};Name={role.Name}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Name)
            .Select(x => new PermissionResponse(x.Id, x.Name, x.Module, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RolePermissionMatrixRow>> GetPermissionMatrixAsync(CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.Roles
            .OrderBy(x => x.Name)
            .Select(x => new { x.Code, x.Name })
            .ToListAsync(cancellationToken);

        var permissions = await dbContext.Permissions
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return permissions.Select(permission =>
        {
            var assignments = roles.ToDictionary(
                role => role.Code,
                role => permission.RolePermissions.Any(rp => rp.Role.Code == role.Code));

            return new RolePermissionMatrixRow(permission.Name, permission.Module, assignments);
        }).ToArray();
    }

    private async Task ReplacePermissionsAsync(AppRole role, IReadOnlyCollection<string> permissionNames, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(existing);

        var permissions = await dbContext.Permissions
            .Where(x => permissionNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }
    }

    private static RoleResponse MapRole(AppRole role)
    {
        return new RoleResponse(
            role.Id,
            role.Code,
            role.Name ?? string.Empty,
            role.Description,
            role.IsSystem,
            role.RolePermissions.Select(x => x.Permission.Name).OrderBy(x => x).ToArray());
    }
}
