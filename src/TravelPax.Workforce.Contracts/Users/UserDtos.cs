namespace TravelPax.Workforce.Contracts.Users;

public sealed record UserListItemResponse(
    Guid Id,
    string EmployeeId,
    string DisplayName,
    string Email,
    string Department,
    string Designation,
    string EmploymentType,
    string Branch,
    string Status,
    DateTimeOffset? LastLoginAt,
    IReadOnlyCollection<string> Roles);

public sealed record UserDetailResponse(
    Guid Id,
    string EmployeeId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? MobileNumber,
    string? Department,
    string? Designation,
    string? EmploymentType,
    DateOnly? DateJoined,
    Guid? ReportingManagerId,
    string? ReportingManagerName,
    Guid? BranchId,
    string? Branch,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string Status,
    DateTimeOffset? LastLoginAt,
    IReadOnlyCollection<string> Roles);

public sealed record UserListResponse(
    IReadOnlyCollection<UserListItemResponse> Items,
    int TotalCount);

public sealed record CreateUserRequest(
    string EmployeeId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? MobileNumber,
    string? Department,
    string? Designation,
    string? EmploymentType,
    DateOnly? DateJoined,
    Guid? ReportingManagerId,
    Guid? BranchId,
    string Status,
    IReadOnlyCollection<string> RoleCodes);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? MobileNumber,
    string? Department,
    string? Designation,
    string? EmploymentType,
    DateOnly? DateJoined,
    Guid? ReportingManagerId,
    Guid? BranchId,
    string Status,
    IReadOnlyCollection<string> RoleCodes);

public sealed record UpdateUserStatusRequest(string Status);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record UpdateMyProfileRequest(
    string DisplayName,
    string? MobileNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone);

public sealed record CreateMyProfileUpdateRequest(
    string DisplayName,
    string? MobileNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string Reason);

public sealed record ProfileUpdateRequestResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    string CurrentDisplayName,
    string? CurrentMobileNumber,
    string? CurrentEmergencyContactName,
    string? CurrentEmergencyContactPhone,
    string RequestedDisplayName,
    string? RequestedMobileNumber,
    string? RequestedEmergencyContactName,
    string? RequestedEmergencyContactPhone,
    string Reason,
    string Status,
    Guid? ReviewedByUserId,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt,
    string? ReviewerComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AppliedAt);

public sealed record ProfileUpdateRequestListResponse(
    IReadOnlyCollection<ProfileUpdateRequestResponse> Items,
    int TotalCount);

public sealed record ReviewProfileUpdateRequest(
    bool Approve,
    string? ReviewerComment);

public sealed record RoleOptionResponse(string Code, string Name);

public sealed record BranchOptionResponse(Guid Id, string Code, string Name);

public sealed record UserAvailabilityResponse(
    bool EmailExists,
    bool EmployeeIdExists);
