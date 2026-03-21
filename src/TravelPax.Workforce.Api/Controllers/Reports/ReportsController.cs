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
        var validationError = ValidateQuery(fromDate, toDate, page, pageSize);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

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
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateQuery(fromDate, toDate, page: 1, pageSize: 20, requirePaginationValidation: false);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var response = await reportService.GetAttendanceTrendAsync(
            fromDate,
            toDate,
            branchId,
            department,
            status,
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
        var validationError = ValidateQuery(fromDate, toDate, page: 1, pageSize: 20, requirePaginationValidation: false);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var csv = await reportService.ExportAttendanceCsvAsync(fromDate, toDate, branchId, department, status, cancellationToken);
        var fileName = $"attendance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet("attendance/export/xlsx")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAttendanceExcel(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? branchId,
        [FromQuery] string? department,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateQuery(fromDate, toDate, page: 1, pageSize: 20, requirePaginationValidation: false);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var fileContent = await reportService.ExportAttendanceExcelAsync(fromDate, toDate, branchId, department, status, cancellationToken);
        var fileName = $"attendance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static string? ValidateQuery(
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        bool requirePaginationValidation = true)
    {
        if (fromDate is not null && toDate is not null && fromDate > toDate)
        {
            return "fromDate must be less than or equal to toDate.";
        }

        if (fromDate is not null && toDate is not null)
        {
            var days = toDate.Value.DayNumber - fromDate.Value.DayNumber + 1;
            if (days > 366)
            {
                return "Date range cannot exceed 366 days.";
            }
        }

        if (!requirePaginationValidation)
        {
            return null;
        }

        if (page < 1)
        {
            return "page must be at least 1.";
        }

        if (pageSize is < 1 or > 200)
        {
            return "pageSize must be between 1 and 200.";
        }

        return null;
    }
}
