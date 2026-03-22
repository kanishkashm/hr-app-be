using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Settings;
using TravelPax.Workforce.Contracts.Settings;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Settings;

[ApiController]
[Route("api/v1/settings")]
[Authorize]
public sealed class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet("overview")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(SettingsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var response = await settingsService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("company")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(CompanySettingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanySettingRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateCompanyAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("branches")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBranch([FromBody] UpsertBranchRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateBranchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOverview), new { }, response);
    }

    [HttpPut("branches/{branchId:guid}")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBranch(Guid branchId, [FromBody] UpsertBranchRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateBranchAsync(branchId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("allowed-networks")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(AllowedNetworkResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAllowedNetwork([FromBody] UpsertAllowedNetworkRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateAllowedNetworkAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOverview), new { }, response);
    }

    [HttpPut("allowed-networks/{networkId:guid}")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(AllowedNetworkResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAllowedNetwork(Guid networkId, [FromBody] UpsertAllowedNetworkRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateAllowedNetworkAsync(networkId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("allowed-networks/test")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(NetworkValidationCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestAllowedNetwork([FromBody] NetworkValidationCheckRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await settingsService.TestNetworkAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("attendance-locks")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendancePeriodLockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceLocks(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var response = await settingsService.GetAttendancePeriodLocksAsync(year, month, branchId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("attendance-locks")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendancePeriodLockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetAttendanceLock([FromBody] SetAttendancePeriodLockRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.SetAttendancePeriodLockAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("payroll-finalizations")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(IReadOnlyCollection<PayrollPeriodFinalizationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayrollFinalizations(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var response = await settingsService.GetPayrollFinalizationsAsync(year, month, branchId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("payroll-finalizations/finalize")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(PayrollPeriodFinalizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FinalizePayrollPeriod([FromBody] FinalizePayrollPeriodRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.FinalizePayrollPeriodAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("payroll-finalizations/{finalizationId:guid}/reopen")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(PayrollPeriodFinalizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReopenPayrollPeriod(
        Guid finalizationId,
        [FromBody] ReopenPayrollPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.ReopenPayrollPeriodAsync(finalizationId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("attendance-rules")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendanceRuleProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceRules(CancellationToken cancellationToken = default)
    {
        var response = await settingsService.GetAttendanceRuleProfilesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("attendance-rules")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceRuleProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAttendanceRule([FromBody] UpsertAttendanceRuleProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.CreateAttendanceRuleProfileAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAttendanceRules), new { }, response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("attendance-rules/{ruleId:guid}")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceRuleProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAttendanceRule(Guid ruleId, [FromBody] UpsertAttendanceRuleProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.UpdateAttendanceRuleProfileAsync(ruleId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("attendance-rules/preview")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [ProducesResponseType(typeof(AttendanceRulePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewAttendanceRule([FromBody] AttendanceRulePreviewRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.PreviewAttendanceRuleAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("work-calendar")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(IReadOnlyCollection<WorkCalendarEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWorkCalendar(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.GetWorkCalendarEntriesAsync(year, month, branchId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("work-calendar")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(WorkCalendarEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorkCalendar([FromBody] UpsertWorkCalendarEntryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.CreateWorkCalendarEntryAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetWorkCalendar), new { year = request.CalendarDate.Year, month = request.CalendarDate.Month, branchId = request.BranchId }, response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("work-calendar/{entryId:guid}")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(WorkCalendarEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateWorkCalendar(Guid entryId, [FromBody] UpsertWorkCalendarEntryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await settingsService.UpdateWorkCalendarEntryAsync(entryId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
