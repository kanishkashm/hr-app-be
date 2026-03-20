using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Attendance;
using TravelPax.Workforce.Contracts.Attendance;

namespace TravelPax.Workforce.Api.Controllers.Attendance;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet("me/today")]
    [ProducesResponseType(typeof(AttendanceTodayResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetToday(CancellationToken cancellationToken)
    {
        var response = await attendanceService.GetMyTodayAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("me/clock-in")]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockIn([FromBody] ClockAttendanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.ClockInAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("me/clock-out")]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockOut([FromBody] ClockAttendanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.ClockOutAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("me/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendanceRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyHistory([FromQuery] int take = 30, CancellationToken cancellationToken = default)
    {
        var response = await attendanceService.GetMyHistoryAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(AttendanceListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? userId,
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await attendanceService.GetAttendanceAsync(
            fromDate,
            toDate,
            userId,
            department,
            status,
            branchId,
            page,
            pageSize,
            cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{attendanceId:guid}/correct")]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CorrectAttendance(
        Guid attendanceId,
        [FromBody] AttendanceCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.CorrectAttendanceAsync(attendanceId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("me/{attendanceId:guid}/correct")]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SelfCorrectAttendance(
        Guid attendanceId,
        [FromBody] AttendanceCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.SelfCorrectAttendanceAsync(attendanceId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
