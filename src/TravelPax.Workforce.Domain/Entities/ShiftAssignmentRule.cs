using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class ShiftAssignmentRule : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public string? Department { get; set; }
    public string? Team { get; set; }
    public Guid ShiftId { get; set; }
    public ShiftDefinition Shift { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

