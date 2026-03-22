using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class UserNotification : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Type { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
