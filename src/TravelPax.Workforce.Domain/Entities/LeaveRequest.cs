using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class LeaveRequest : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string LeaveType { get; set; } = "Annual";
    public string DayPortion { get; set; } = "FullDay";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewerNote { get; set; }
}
