using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Shifts;
using TravelPax.Workforce.Contracts.Shifts;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Shifts;

[ApiController]
[Route("api/v1/shifts")]
[Authorize]
public sealed class ShiftsController(IShiftService shiftService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ShiftsView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShifts([FromQuery] Guid? branchId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var response = await shiftService.GetShiftsAsync(branchId, isActive, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateShift([FromBody] UpsertShiftRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.CreateShiftAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{shiftId:guid}")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateShift(Guid shiftId, [FromBody] UpsertShiftRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.UpdateShiftAsync(shiftId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("assignments")]
    [Authorize(Policy = PermissionCodes.ShiftsView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments([FromQuery] Guid? userId, [FromQuery] Guid? shiftId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var response = await shiftService.GetAssignmentsAsync(userId, shiftId, isActive, cancellationToken);
        return Ok(response);
    }

    [HttpPost("assignments")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAssignment([FromBody] UpsertShiftAssignmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.CreateAssignmentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("assignments/{assignmentId:guid}")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAssignment(Guid assignmentId, [FromBody] UpsertShiftAssignmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.UpdateAssignmentAsync(assignmentId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("assignment-rules")]
    [Authorize(Policy = PermissionCodes.ShiftsView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftAssignmentRuleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules([FromQuery] Guid? branchId, [FromQuery] string? department, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var response = await shiftService.GetAssignmentRulesAsync(branchId, department, isActive, cancellationToken);
        return Ok(response);
    }

    [HttpPost("assignment-rules")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftAssignmentRuleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRule([FromBody] UpsertShiftAssignmentRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.CreateAssignmentRuleAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("assignment-rules/{ruleId:guid}")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftAssignmentRuleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRule(Guid ruleId, [FromBody] UpsertShiftAssignmentRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.UpdateAssignmentRuleAsync(ruleId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("overrides")]
    [Authorize(Policy = PermissionCodes.ShiftsView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftOverrideResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverrides([FromQuery] Guid? userId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken cancellationToken)
    {
        var response = await shiftService.GetOverridesAsync(userId, fromDate, toDate, cancellationToken);
        return Ok(response);
    }

    [HttpPost("overrides")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftOverrideResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOverride([FromBody] UpsertShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.CreateOverrideAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("overrides/{overrideId:guid}")]
    [Authorize(Policy = PermissionCodes.ShiftsManage)]
    [ProducesResponseType(typeof(ShiftOverrideResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOverride(Guid overrideId, [FromBody] UpsertShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.UpdateOverrideAsync(overrideId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("calendar")]
    [Authorize(Policy = PermissionCodes.ShiftsView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftCalendarItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await shiftService.GetCalendarAsync(fromDate, toDate, branchId, userId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}

