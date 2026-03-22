using TravelPax.Workforce.Contracts.Managers;

namespace TravelPax.Workforce.Application.Abstractions.Managers;

public interface IManagerService
{
    Task<ManagerDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
}
