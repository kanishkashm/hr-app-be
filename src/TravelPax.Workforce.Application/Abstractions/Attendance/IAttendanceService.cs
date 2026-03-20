using TravelPax.Workforce.Contracts.Attendance;

namespace TravelPax.Workforce.Application.Abstractions.Attendance;

public interface IAttendanceService
{
    Task<AttendanceTodayResponse> GetMyTodayAsync(CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> ClockInAsync(ClockAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> ClockOutAsync(ClockAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceRecordResponse>> GetMyHistoryAsync(int take, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> CorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceListResponse> GetAttendanceAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? userId,
        string? department,
        string? status,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
