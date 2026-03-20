using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class CompanySetting : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = "TravelPax";
    public string DefaultTimezone { get; set; } = "Asia/Colombo";
    public TimeOnly WorkingDayStartTime { get; set; } = new(9, 0);
    public TimeOnly WorkingDayEndTime { get; set; } = new(18, 0);
    public int LateGraceMinutes { get; set; } = 15;
    public string WeekendConfig { get; set; } = "{\"days\":[\"Saturday\",\"Sunday\"]}";
}
