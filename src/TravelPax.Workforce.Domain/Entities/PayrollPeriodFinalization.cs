using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class PayrollPeriodFinalization : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public bool IsFinalized { get; set; } = true;
    public DateTimeOffset FinalizedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? FinalizedByUserId { get; set; }
    public AppUser? FinalizedByUser { get; set; }
    public DateTimeOffset? ReopenedAt { get; set; }
    public Guid? ReopenedByUserId { get; set; }
    public AppUser? ReopenedByUser { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string? Notes { get; set; }
}
