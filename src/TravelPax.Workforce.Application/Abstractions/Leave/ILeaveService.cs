using TravelPax.Workforce.Contracts.Leave;

namespace TravelPax.Workforce.Application.Abstractions.Leave;

public interface ILeaveService
{
    Task<LeaveRequestResponse> CreateMyRequestAsync(LeaveRequestCreateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaveRequestResponse>> GetMyRequestsAsync(int take, CancellationToken cancellationToken = default);
    Task<LeaveRequestListResponse> GetRequestsAsync(string? status, Guid? userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponse> ReviewRequestAsync(Guid requestId, LeaveRequestReviewRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponse> ReviewRequestByHrAsync(Guid requestId, LeaveRequestReviewRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequestResponse> ReviewRequestByDirectorAsync(Guid requestId, LeaveRequestReviewRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaveBalanceResponse>> GetMyBalancesAsync(int? year, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaveBalanceResponse>> GetBalancesAsync(int? year, Guid? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeavePolicyResponse>> GetPoliciesAsync(CancellationToken cancellationToken = default);
    Task<LeavePolicyResponse> UpsertPolicyAsync(Guid? policyId, LeavePolicyUpsertRequest request, CancellationToken cancellationToken = default);
}
