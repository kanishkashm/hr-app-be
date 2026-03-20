using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AllowedNetwork : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public OfficeBranch Branch { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string NetworkType { get; set; } = "Cidr";
    public string IpOrCidr { get; set; } = string.Empty;
    public string ValidationMode { get; set; } = "Allow";
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
}
