using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Networking;

namespace TravelPax.Workforce.UnitTests;

public sealed class NetworkValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsInsideOffice_WhenExactIpRuleMatches()
    {
        await using var db = TestDbFactory.CreateContext();
        var branchId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        db.OfficeBranches.Add(new OfficeBranch
        {
            Id = branchId,
            Code = "CMB-HQ",
            Name = "Colombo HQ",
        });
        db.AllowedNetworks.Add(new AllowedNetwork
        {
            Id = networkId,
            BranchId = branchId,
            Name = "Public IP",
            IpOrCidr = "112.134.207.60",
            NetworkType = "PublicIp",
            ValidationMode = "Allow",
            Priority = 1,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new NetworkValidationService(db);

        var result = await service.ValidateAsync(branchId, "112.134.207.60");

        Assert.Equal("InsideOfficeNetwork", result.Status);
        Assert.Equal(networkId, result.MatchedRuleId);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInsideOffice_WhenCidrRuleMatches()
    {
        await using var db = TestDbFactory.CreateContext();
        var branchId = Guid.NewGuid();
        db.OfficeBranches.Add(new OfficeBranch
        {
            Id = branchId,
            Code = "CMB-HQ",
            Name = "Colombo HQ",
        });
        db.AllowedNetworks.Add(new AllowedNetwork
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            Name = "CIDR",
            IpOrCidr = "203.94.76.0/24",
            NetworkType = "Cidr",
            ValidationMode = "Allow",
            Priority = 1,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new NetworkValidationService(db);

        var result = await service.ValidateAsync(branchId, "203.94.76.25");

        Assert.Equal("InsideOfficeNetwork", result.Status);
        Assert.NotNull(result.MatchedRuleId);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsUnknown_WhenIpIsInvalid()
    {
        await using var db = TestDbFactory.CreateContext();
        var service = new NetworkValidationService(db);

        var result = await service.ValidateAsync(Guid.NewGuid(), "not-an-ip");

        Assert.Equal("Unknown", result.Status);
        Assert.Null(result.MatchedRuleId);
    }
}
