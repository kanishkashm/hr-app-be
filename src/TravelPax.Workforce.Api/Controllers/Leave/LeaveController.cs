using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Leave;
using TravelPax.Workforce.Contracts.Leave;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Leave;

[ApiController]
[Route("api/v1/leave")]
[Authorize]
public sealed class LeaveController(ILeaveService leaveService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = PermissionCodes.LeaveRequest)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LeaveRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests([FromQuery] int take = 30, CancellationToken cancellationToken = default)
    {
        var response = await leaveService.GetMyRequestsAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpGet("balances/me")]
    [Authorize(Policy = PermissionCodes.LeaveRequest)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LeaveBalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBalances([FromQuery] int? year, CancellationToken cancellationToken = default)
    {
        var response = await leaveService.GetMyBalancesAsync(year, cancellationToken);
        return Ok(response);
    }

    [HttpPost("me")]
    [Authorize(Policy = PermissionCodes.LeaveRequest)]
    [ProducesResponseType(typeof(LeaveRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMyRequest(
        [FromBody] LeaveRequestCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await leaveService.CreateMyRequestAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.LeaveView)]
    [ProducesResponseType(typeof(LeaveRequestListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? status,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await leaveService.GetRequestsAsync(status, userId, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{requestId:guid}/review")]
    [Authorize(Policy = PermissionCodes.LeaveManage)]
    [ProducesResponseType(typeof(LeaveRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewRequest(
        Guid requestId,
        [FromBody] LeaveRequestReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await leaveService.ReviewRequestAsync(requestId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("balances")]
    [Authorize(Policy = PermissionCodes.LeaveView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LeaveBalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalances(
        [FromQuery] int? year,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var response = await leaveService.GetBalancesAsync(year, userId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("policies")]
    [Authorize(Policy = PermissionCodes.LeaveView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LeavePolicyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPolicies(CancellationToken cancellationToken = default)
    {
        var response = await leaveService.GetPoliciesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("policies/{policyId:guid}")]
    [Authorize(Policy = PermissionCodes.LeaveManage)]
    [ProducesResponseType(typeof(LeavePolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePolicy(
        Guid policyId,
        [FromBody] LeavePolicyUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await leaveService.UpsertPolicyAsync(policyId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("policies")]
    [Authorize(Policy = PermissionCodes.LeaveManage)]
    [ProducesResponseType(typeof(LeavePolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePolicy(
        [FromBody] LeavePolicyUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await leaveService.UpsertPolicyAsync(null, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
