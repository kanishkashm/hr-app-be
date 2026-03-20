using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class LoginAuditLog : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? EmailOrUsername { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceSummary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset? LogoutAt { get; set; }
}
