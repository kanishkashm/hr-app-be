using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Notifications;
using TravelPax.Workforce.Contracts.Notifications;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Notifications;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(NotificationSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var response = await notificationService.GetMyNotificationsAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await notificationService.MarkAsReadAsync(notificationId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("me/read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("outbox/health")]
    [Authorize(Policy = PermissionCodes.AuditView)]
    [ProducesResponseType(typeof(EmailOutboxHealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxHealth(CancellationToken cancellationToken = default)
    {
        var response = await notificationService.GetEmailOutboxHealthAsync(cancellationToken);
        return Ok(response);
    }
}
