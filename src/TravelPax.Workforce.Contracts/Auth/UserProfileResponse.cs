namespace TravelPax.Workforce.Contracts.Auth;

public sealed record UserProfileResponse(
    Guid Id,
    string EmployeeId,
    string Email,
    string DisplayName,
    string Department,
    string Designation,
    string Branch,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
