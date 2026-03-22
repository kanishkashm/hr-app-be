using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class UserProfileUpdateRequest : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;
    public string CurrentDisplayName { get; set; } = string.Empty;
    public string? CurrentMobileNumber { get; set; }
    public string? CurrentEmergencyContactName { get; set; }
    public string? CurrentEmergencyContactPhone { get; set; }
    public string RequestedDisplayName { get; set; } = string.Empty;
    public string? RequestedMobileNumber { get; set; }
    public string? RequestedEmergencyContactName { get; set; }
    public string? RequestedEmergencyContactPhone { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewerComment { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
}
