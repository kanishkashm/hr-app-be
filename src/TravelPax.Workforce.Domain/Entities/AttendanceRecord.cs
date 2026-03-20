using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AttendanceRecord : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset? ClockInAt { get; set; }
    public DateTimeOffset? ClockOutAt { get; set; }
    public int? TotalWorkMinutes { get; set; }
    public string Status { get; set; } = "PendingClockOut";
    public bool IsLate { get; set; }
    public int? LateMinutes { get; set; }
    public string? ClockInIp { get; set; }
    public string? ClockOutIp { get; set; }
    public string? ClockInUserAgent { get; set; }
    public string? ClockOutUserAgent { get; set; }
    public string? ClockInDeviceSummary { get; set; }
    public string? ClockOutDeviceSummary { get; set; }
    public string? ClockInNetworkValidation { get; set; }
    public string? ClockOutNetworkValidation { get; set; }
    public Guid? ClockInNetworkRuleId { get; set; }
    public AllowedNetwork? ClockInNetworkRule { get; set; }
    public Guid? ClockOutNetworkRuleId { get; set; }
    public AllowedNetwork? ClockOutNetworkRule { get; set; }
    public string? Notes { get; set; }
}
