namespace TravelPax.Workforce.Domain.Entities;

public sealed class UserPermission
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsGranted { get; set; } = true;
}
