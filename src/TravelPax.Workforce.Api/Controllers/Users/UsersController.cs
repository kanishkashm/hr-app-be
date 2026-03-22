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

    [HttpPost("me/profile-update-requests")]
    [ProducesResponseType(typeof(ProfileUpdateRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitMyProfileUpdateRequest([FromBody] CreateMyProfileUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await userService.SubmitMyProfileUpdateRequestAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetMyProfileUpdateRequests), new { }, response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("me/profile-update-requests")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProfileUpdateRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfileUpdateRequests([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var response = await userService.GetMyProfileUpdateRequestsAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpGet("profile-update-requests")]
    [Authorize(Policy = PermissionCodes.UsersEdit)]
    [ProducesResponseType(typeof(ProfileUpdateRequestListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfileUpdateRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await userService.GetProfileUpdateRequestsAsync(status, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("profile-update-requests/{requestId:guid}/review")]
    [Authorize(Policy = PermissionCodes.UsersEdit)]
    [ProducesResponseType(typeof(ProfileUpdateRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewProfileUpdateRequest(Guid requestId, [FromBody] ReviewProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await userService.ReviewProfileUpdateRequestAsync(requestId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
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
