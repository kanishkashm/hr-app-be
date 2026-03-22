using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class LeavePolicy : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string LeaveType { get; set; } = "Annual";
    public string EmploymentType { get; set; } = "FullTime";
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public int AnnualAllocationDays { get; set; } = 14;
    public int MaxCarryForwardDays { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
