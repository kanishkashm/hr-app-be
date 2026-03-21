namespace TravelPax.Workforce.Contracts.Reports;

public sealed record AttendanceReportItemResponse(
    Guid AttendanceId,
    DateOnly AttendanceDate,
    Guid UserId,
    string EmployeeId,
    string EmployeeName,
    string Department,
    string Designation,
    string Branch,
    DateTimeOffset? ClockInAt,
    DateTimeOffset? ClockOutAt,
    int? TotalWorkMinutes,
    string Status,
    bool IsLate,
    string? ClockInNetworkValidation,
    string? ClockOutNetworkValidation);

public sealed record AttendanceReportSummaryResponse(
    int TotalRecords,
    int TotalPresent,
    int TotalLate,
    int TotalPendingClockOut,
    int UniqueEmployees,
    double AverageWorkHours);

public sealed record AttendanceTrendPointResponse(
    DateOnly Date,
    int Present,
    int Late,
    int PendingClockOut,
    int TotalClockedIn);

public sealed record AttendanceReportResponse(
    IReadOnlyCollection<AttendanceReportItemResponse> Items,
    int TotalCount,
    AttendanceReportSummaryResponse Summary);
