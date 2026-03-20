using System.Net;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TravelPax.Workforce.Infrastructure.Networking;

public sealed class NetworkValidationService(TravelPaxDbContext dbContext) : INetworkValidationService
{
    public async Task<NetworkValidationResult> ValidateAsync(Guid? branchId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (branchId is null || string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out var requestIp))
        {
            return new NetworkValidationResult("Unknown", null);
        }

        var rules = await dbContext.AllowedNetworks
            .Where(x => x.BranchId == branchId && x.IsActive)
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (IsMatch(requestIp, rule.IpOrCidr))
            {
                return new NetworkValidationResult("InsideOfficeNetwork", rule.Id);
            }
        }

        return new NetworkValidationResult("OutsideOfficeNetwork", null);
    }

    private static bool IsMatch(IPAddress requestIp, string ipOrCidr)
    {
        if (!ipOrCidr.Contains('/'))
        {
            return IPAddress.TryParse(ipOrCidr, out var exactIp) && exactIp.Equals(requestIp);
        }

        var parts = ipOrCidr.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkIp) || !int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        var requestBytes = requestIp.GetAddressBytes();
        var networkBytes = networkIp.GetAddressBytes();
        if (requestBytes.Length != networkBytes.Length)
        {
            return false;
        }

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (requestBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)~(255 >> remainingBits);
        return (requestBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }
}
