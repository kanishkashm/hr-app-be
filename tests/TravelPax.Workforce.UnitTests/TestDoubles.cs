using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.UnitTests;

internal static class TestDbFactory
{
    internal static TravelPaxDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TravelPaxDbContext>()
            .UseInMemoryDatabase($"travelpax-tests-{Guid.NewGuid():N}")
            .Options;

        return new TravelPaxDbContext(options);
    }
}

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    private readonly Guid? _userId;

    public FakeCurrentUserService(Guid? userId)
    {
        _userId = userId;
    }

    public Guid? UserId => _userId;
    public string? Email => _userId is null ? null : "tester@travelpax.lk";
    public bool IsAuthenticated => _userId is not null;
}

internal sealed class FakeNetworkValidationService(NetworkValidationResult? result = null) : INetworkValidationService
{
    private readonly NetworkValidationResult _result = result ?? new NetworkValidationResult("InsideOfficeNetwork", null);

    public Task<NetworkValidationResult> ValidateAsync(Guid? branchId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_result);
    }
}
