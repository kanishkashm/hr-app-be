using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Roles;
using TravelPax.Workforce.Contracts.Roles;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Roles;

[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public sealed class PermissionsController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.RolesView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var response = await roleService.GetPermissionsAsync(cancellationToken);
        return Ok(response);
    }
}
