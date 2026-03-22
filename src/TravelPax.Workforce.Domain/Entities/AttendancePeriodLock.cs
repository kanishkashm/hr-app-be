using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AttendancePeriodLock : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public bool IsLocked { get; set; } = true;
    public DateTimeOffset LockedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? LockedByUserId { get; set; }
    public AppUser? LockedByUser { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
    public Guid? UnlockedByUserId { get; set; }
    public AppUser? UnlockedByUser { get; set; }
    public string? Notes { get; set; }
}
