namespace TravelPax.Workforce.Contracts.Auth;

public sealed record UserProfileResponse(
    Guid Id,
    string EmployeeId,
    string Email,
    string DisplayName,
    string Department,
    string Designation,
    string Branch,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
