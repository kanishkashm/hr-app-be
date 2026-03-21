using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Reports;
using TravelPax.Workforce.Contracts.Reports;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Reports;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("attendance")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    [ProducesResponseType(typeof(AttendanceReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceReport(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? branchId,
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await reportService.GetAttendanceReportAsync(
            fromDate,
            toDate,
            branchId,
            department,
            status,
            page,
            pageSize,
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("attendance/trend")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendanceTrendPointResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceTrend(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? branchId,
        [FromQuery] string? department,
        CancellationToken cancellationToken = default)
    {
        var response = await reportService.GetAttendanceTrendAsync(
            fromDate,
            toDate,
            branchId,
            department,
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("attendance/export")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAttendanceCsv(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? branchId,
        [FromQuery] string? department,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var csv = await reportService.ExportAttendanceCsvAsync(fromDate, toDate, branchId, department, status, cancellationToken);
        var fileName = $"attendance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }
}
