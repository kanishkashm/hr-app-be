using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class WorkCalendarEntry : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public DateOnly CalendarDate { get; set; }
    public string DayType { get; set; } = "PublicHoliday";
    public string Name { get; set; } = string.Empty;
    public bool IsRecurringAnnual { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
