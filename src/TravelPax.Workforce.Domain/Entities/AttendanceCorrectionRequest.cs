using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AttendanceCorrectionRequest : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = null!;
    public Guid RequestedByUserId { get; set; }
    public AppUser RequestedByUser { get; set; } = null!;
    public string RequestType { get; set; } = "Correction";
    public DateTimeOffset RequestedClockInAt { get; set; }
    public DateTimeOffset? RequestedClockOutAt { get; set; }
    public string? RequestedNotes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewerNote { get; set; }
    public string? ReviewIpAddress { get; set; }
}
