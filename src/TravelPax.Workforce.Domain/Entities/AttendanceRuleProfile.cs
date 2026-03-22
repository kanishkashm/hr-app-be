using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AttendanceRuleProfile : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "Global"; // Global | Branch | Shift
    public Guid? BranchId { get; set; }
    public Guid? ShiftId { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    public int? LateGraceMinutes { get; set; }
    public int? HalfDayThresholdMinutes { get; set; }
    public int? MinPresentMinutes { get; set; }
    public int? OvertimeThresholdMinutes { get; set; }
    public int? EarlyOutGraceMinutes { get; set; }
    public int? ShortLeaveDeductionMinutes { get; set; }
    public bool EnableMissedPunchDetection { get; set; } = true;

    public OfficeBranch? Branch { get; set; }
    public ShiftDefinition? Shift { get; set; }
}
