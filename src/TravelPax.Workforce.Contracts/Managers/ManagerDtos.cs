namespace TravelPax.Workforce.Contracts.Managers;

public sealed record ManagerTeamAttendanceItemResponse(
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    string Department,
    string Designation,
    string Branch,
    string Status,
    DateTimeOffset? ClockInAt,
    DateTimeOffset? ClockOutAt);

public sealed record ManagerDashboardResponse(
    int TeamSize,
    int PresentToday,
    int LateToday,
    int AbsentOrNotClockedToday,
    int PendingAttendanceApprovals,
    int PendingLeaveApprovals,
    int PendingProfileApprovals,
    IReadOnlyCollection<ManagerTeamAttendanceItemResponse> TeamToday);
