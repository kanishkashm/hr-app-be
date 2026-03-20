namespace TravelPax.Workforce.Contracts.Attendance;

public sealed record AttendanceRecordResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    string Department,
    string Designation,
    string Branch,
    DateOnly AttendanceDate,
    DateTimeOffset? ClockInAt,
    DateTimeOffset? ClockOutAt,
    int? TotalWorkMinutes,
    string Status,
    bool IsLate,
    int? LateMinutes,
    string? ClockInIp,
    string? ClockOutIp,
    string? ClockInNetworkValidation,
    string? ClockOutNetworkValidation,
    string? Notes);

public sealed record AttendanceTodayResponse(
    DateOnly Date,
    string Timezone,
    string Status,
    bool CanClockIn,
    bool CanClockOut,
    AttendanceRecordResponse? Record);

public sealed record ClockAttendanceRequest(string? Notes);

public sealed record AttendanceCorrectionRequest(
    DateTimeOffset? ClockInAt,
    DateTimeOffset? ClockOutAt,
    string Status,
    string? Notes,
    string Reason);

public sealed record AttendanceSummaryResponse(
    int TotalEmployees,
    int PresentToday,
    int AbsentToday,
    int LateToday,
    int PendingClockOut);

public sealed record AttendanceListResponse(
    IReadOnlyCollection<AttendanceRecordResponse> Items,
    int TotalCount,
    AttendanceSummaryResponse Summary);
