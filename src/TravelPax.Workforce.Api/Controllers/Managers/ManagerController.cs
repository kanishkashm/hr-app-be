using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Managers;
using TravelPax.Workforce.Contracts.Managers;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Managers;

[ApiController]
[Route("api/v1/manager")]
[Authorize]
public sealed class ManagerController(IManagerService managerService) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Policy = PermissionCodes.AttendanceView)]
    [ProducesResponseType(typeof(ManagerDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        var response = await managerService.GetDashboardAsync(cancellationToken);
        return Ok(response);
    }
}
