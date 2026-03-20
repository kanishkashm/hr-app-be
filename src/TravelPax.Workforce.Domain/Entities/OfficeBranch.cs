using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class OfficeBranch : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string Country { get; set; } = "Sri Lanka";
    public string Timezone { get; set; } = "Asia/Colombo";
    public bool IsActive { get; set; } = true;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<AllowedNetwork> AllowedNetworks { get; set; } = new List<AllowedNetwork>();
}
