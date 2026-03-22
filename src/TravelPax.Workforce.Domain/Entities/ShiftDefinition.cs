using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class ShiftDefinition : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public string ShiftType { get; set; } = "Fixed";
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public TimeOnly EndTime { get; set; } = new(18, 0);
    public int FlexMinutes { get; set; }
    public int GraceMinutes { get; set; } = 15;
    public int MinHalfDayMinutes { get; set; } = 240;
    public int MinPresentMinutes { get; set; } = 480;
    public int OvertimeAfterMinutes { get; set; } = 540;
    public bool IsActive { get; set; } = true;
}

