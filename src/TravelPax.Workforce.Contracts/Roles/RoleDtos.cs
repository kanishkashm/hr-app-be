namespace TravelPax.Workforce.Contracts.Roles;

public sealed record PermissionResponse(
    Guid Id,
    string Name,
    string Module,
    string? Description);

public sealed record RoleResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyCollection<string> Permissions);

public sealed record RoleListResponse(IReadOnlyCollection<RoleResponse> Items);

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    string? Description,
    IReadOnlyCollection<string> PermissionNames);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<string> PermissionNames);

public sealed record RolePermissionMatrixRow(
    string PermissionName,
    string Module,
    IReadOnlyDictionary<string, bool> RoleAssignments);
