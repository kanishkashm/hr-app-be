using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class LeaveBalance : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int Year { get; set; }
    public string LeaveType { get; set; } = "Annual";
    public decimal AllocatedDays { get; set; } = 0;
    public decimal CarryForwardDays { get; set; } = 0;
    public decimal ManualAdjustmentDays { get; set; } = 0;
    public decimal UsedDays { get; set; } = 0;
}
