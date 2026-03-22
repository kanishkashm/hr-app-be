using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class ShiftOverride : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public Guid ShiftId { get; set; }
    public ShiftDefinition Shift { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Approved";
}

