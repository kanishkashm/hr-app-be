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
    string? DetectedIp,
    string Status,
    bool CanClockIn,
    bool CanClockOut,
    AttendanceRecordResponse? Record);

public sealed record ClockAttendanceRequest(string? Notes);

public sealed record AttendanceCorrectionUpdateRequest(
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

public sealed record AttendanceDetailResponse(
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
    string? ClockInUserAgent,
    string? ClockOutUserAgent,
    string? ClockInDeviceSummary,
    string? ClockOutDeviceSummary,
    string? ClockInNetworkValidation,
    string? ClockOutNetworkValidation,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? CreatedBy,
    Guid? UpdatedBy);

public sealed record AttendanceCorrectionSubmissionRequest(
    DateTimeOffset ClockInAt,
    DateTimeOffset? ClockOutAt,
    string? Notes,
    string Reason,
    string? RequestType = null);

public sealed record AttendanceCorrectionReviewRequest(
    bool Approve,
    string? ReviewerNote);

public sealed record AttendanceCorrectionRequestResponse(
    Guid Id,
    Guid AttendanceRecordId,
    Guid RequestedByUserId,
    string RequestedByEmployee,
    DateOnly AttendanceDate,
    string RequestType,
    DateTimeOffset RequestedClockInAt,
    DateTimeOffset? RequestedClockOutAt,
    string? RequestedNotes,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? ReviewedByUserId,
    string? ReviewedByEmployee,
    DateTimeOffset? ReviewedAt,
    string? ReviewerNote);

public sealed record AttendanceCorrectionRequestListResponse(
    IReadOnlyCollection<AttendanceCorrectionRequestResponse> Items,
    int TotalCount);

public sealed record AttendanceExceptionTimelineEventResponse(
    DateTimeOffset At,
    string EventType,
    string Actor,
    string? Note);

public sealed record AttendanceExceptionDetailResponse(
    AttendanceCorrectionRequestResponse Request,
    IReadOnlyCollection<AttendanceExceptionTimelineEventResponse> Timeline);
