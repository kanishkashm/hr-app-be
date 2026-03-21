using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Users;
using TravelPax.Workforce.Contracts.Users;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Users;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.UsersView)]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await userService.GetUsersAsync(search, status, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = PermissionCodes.UsersView)]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var response = await userService.GetUserAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.UsersCreate)]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { userId = response.Id }, response);
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Policy = PermissionCodes.UsersEdit)]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateUserAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{userId:guid}/status")]
    [Authorize(Policy = PermissionCodes.UsersEdit)]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(Guid userId, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateUserStatusAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{userId:guid}/reset-password")]
    [Authorize(Policy = PermissionCodes.UsersEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/profile")]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var response = await userService.GetMyProfileAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("me/profile")]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateMyProfileAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("meta/roles")]
    [Authorize(Policy = PermissionCodes.UsersView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await userService.GetRoleOptionsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("meta/branches")]
    [Authorize(Policy = PermissionCodes.UsersView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<BranchOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranches(CancellationToken cancellationToken)
    {
        var response = await userService.GetBranchOptionsAsync(cancellationToken);
        return Ok(response);
    }
}
