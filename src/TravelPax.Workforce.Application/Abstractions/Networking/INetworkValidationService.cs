namespace TravelPax.Workforce.Application.Abstractions.Networking;

public interface INetworkValidationService
{
    Task<NetworkValidationResult> ValidateAsync(Guid? branchId, string? ipAddress, CancellationToken cancellationToken = default);
}
