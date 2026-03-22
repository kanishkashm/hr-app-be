using TravelPax.Workforce.Contracts.Users;

namespace TravelPax.Workforce.Application.Abstractions.Users;

public interface IUserService
{
    Task<UserListResponse> GetUsersAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserDetailResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDetailResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailResponse> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailResponse> GetMyProfileAsync(CancellationToken cancellationToken = default);
    Task<UserDetailResponse> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default);
    Task<ProfileUpdateRequestResponse> SubmitMyProfileUpdateRequestAsync(CreateMyProfileUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProfileUpdateRequestResponse>> GetMyProfileUpdateRequestsAsync(int take, CancellationToken cancellationToken = default);
    Task<ProfileUpdateRequestListResponse> GetProfileUpdateRequestsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProfileUpdateRequestResponse> ReviewProfileUpdateRequestAsync(Guid requestId, ReviewProfileUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleOptionResponse>> GetRoleOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BranchOptionResponse>> GetBranchOptionsAsync(CancellationToken cancellationToken = default);
}
