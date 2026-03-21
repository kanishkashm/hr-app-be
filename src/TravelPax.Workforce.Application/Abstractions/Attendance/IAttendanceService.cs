using TravelPax.Workforce.Contracts.Attendance;

namespace TravelPax.Workforce.Application.Abstractions.Attendance;

public interface IAttendanceService
{
    Task<AttendanceTodayResponse> GetMyTodayAsync(CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> ClockInAsync(ClockAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> ClockOutAsync(ClockAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceRecordResponse>> GetMyHistoryAsync(int take, CancellationToken cancellationToken = default);
    Task<AttendanceDetailResponse> GetAttendanceDetailAsync(Guid attendanceId, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> CorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> SelfCorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionRequestResponse> SubmitCorrectionRequestAsync(Guid attendanceId, AttendanceCorrectionSubmissionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceCorrectionRequestResponse>> GetMyCorrectionRequestsAsync(int take, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionRequestListResponse> GetCorrectionRequestsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionRequestResponse> ReviewCorrectionRequestAsync(Guid requestId, AttendanceCorrectionReviewRequest request, CancellationToken cancellationToken = default);
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
