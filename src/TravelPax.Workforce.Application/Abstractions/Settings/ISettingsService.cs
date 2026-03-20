using TravelPax.Workforce.Contracts.Settings;

namespace TravelPax.Workforce.Application.Abstractions.Settings;

public interface ISettingsService
{
    Task<SettingsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<CompanySettingResponse> UpdateCompanyAsync(UpdateCompanySettingRequest request, CancellationToken cancellationToken = default);
    Task<BranchResponse> CreateBranchAsync(UpsertBranchRequest request, CancellationToken cancellationToken = default);
    Task<BranchResponse> UpdateBranchAsync(Guid branchId, UpsertBranchRequest request, CancellationToken cancellationToken = default);
    Task<AllowedNetworkResponse> CreateAllowedNetworkAsync(UpsertAllowedNetworkRequest request, CancellationToken cancellationToken = default);
    Task<AllowedNetworkResponse> UpdateAllowedNetworkAsync(Guid networkId, UpsertAllowedNetworkRequest request, CancellationToken cancellationToken = default);
}
