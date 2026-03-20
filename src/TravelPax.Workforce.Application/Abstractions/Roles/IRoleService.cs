using TravelPax.Workforce.Contracts.Roles;

namespace TravelPax.Workforce.Application.Abstractions.Roles;

public interface IRoleService
{
    Task<RoleListResponse> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleResponse> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleResponse> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RolePermissionMatrixRow>> GetPermissionMatrixAsync(CancellationToken cancellationToken = default);
}
