using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Roles;
using TravelPax.Workforce.Contracts.Roles;

namespace TravelPax.Workforce.Api.Controllers.Roles;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(RoleListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await roleService.GetRolesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{roleId:guid}")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRole(Guid roleId, CancellationToken cancellationToken)
    {
        var response = await roleService.GetRoleAsync(roleId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await roleService.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRole), new { roleId = response.Id }, response);
    }

    [HttpPut("{roleId:guid}")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await roleService.UpdateRoleAsync(roleId, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRole(Guid roleId, CancellationToken cancellationToken)
    {
        await roleService.DeleteRoleAsync(roleId, cancellationToken);
        return NoContent();
    }

    [HttpGet("permission-matrix")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RolePermissionMatrixRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionMatrix(CancellationToken cancellationToken)
    {
        var response = await roleService.GetPermissionMatrixAsync(cancellationToken);
        return Ok(response);
    }
}
