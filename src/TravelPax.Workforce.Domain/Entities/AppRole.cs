using Microsoft.AspNetCore.Identity;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AppRole : IdentityRole<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
