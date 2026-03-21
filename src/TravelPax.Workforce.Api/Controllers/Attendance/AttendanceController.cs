using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Attendance;
using TravelPax.Workforce.Contracts.Attendance;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Attendance;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet("me/today")]
    [Authorize(Policy = PermissionCodes.AttendanceClock)]
    [ProducesResponseType(typeof(AttendanceTodayResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetToday(CancellationToken cancellationToken)
    {
        var response = await attendanceService.GetMyTodayAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("me/clock-in")]
    [Authorize(Policy = PermissionCodes.AttendanceClock)]
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
    [Authorize(Policy = PermissionCodes.AttendanceClock)]
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
    [Authorize(Policy = PermissionCodes.AttendanceClock)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendanceRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyHistory([FromQuery] int take = 30, CancellationToken cancellationToken = default)
    {
        var response = await attendanceService.GetMyHistoryAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.AttendanceView)]
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

    [HttpGet("{attendanceId:guid}")]
    [Authorize(Policy = PermissionCodes.AttendanceView)]
    [ProducesResponseType(typeof(AttendanceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAttendanceDetail(Guid attendanceId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.GetAttendanceDetailAsync(attendanceId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("{attendanceId:guid}/correct")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CorrectAttendance(
        Guid attendanceId,
        [FromBody] AttendanceCorrectionUpdateRequest request,
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
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SelfCorrectAttendance(
        Guid attendanceId,
        [FromBody] AttendanceCorrectionUpdateRequest request,
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

    [HttpPost("me/{attendanceId:guid}/correction-requests")]
    [Authorize(Policy = PermissionCodes.AttendanceClock)]
    [ProducesResponseType(typeof(AttendanceCorrectionRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitCorrectionRequest(
        Guid attendanceId,
        [FromBody] AttendanceCorrectionSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.SubmitCorrectionRequestAsync(attendanceId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("me/correction-requests")]
    [Authorize(Policy = PermissionCodes.AttendanceClock)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendanceCorrectionRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCorrectionRequests([FromQuery] int take = 30, CancellationToken cancellationToken = default)
    {
        var response = await attendanceService.GetMyCorrectionRequestsAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpGet("correction-requests")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceCorrectionRequestListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCorrectionRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await attendanceService.GetCorrectionRequestsAsync(status, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("correction-requests/{requestId:guid}/review")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceCorrectionRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewCorrectionRequest(
        Guid requestId,
        [FromBody] AttendanceCorrectionReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await attendanceService.ReviewCorrectionRequestAsync(requestId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
