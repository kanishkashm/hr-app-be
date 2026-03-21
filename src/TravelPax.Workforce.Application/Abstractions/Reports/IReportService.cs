using TravelPax.Workforce.Contracts.Reports;

namespace TravelPax.Workforce.Application.Abstractions.Reports;

public interface IReportService
{
    Task<AttendanceReportResponse> GetAttendanceReportAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AttendanceTrendPointResponse>> GetAttendanceTrendAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken = default);

    Task<string> ExportAttendanceCsvAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportAttendanceExcelAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken = default);
}
