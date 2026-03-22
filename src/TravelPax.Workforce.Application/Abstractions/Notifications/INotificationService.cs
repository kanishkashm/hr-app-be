using TravelPax.Workforce.Contracts.Notifications;

namespace TravelPax.Workforce.Application.Abstractions.Notifications;

public interface INotificationService
{
    Task<NotificationSummaryResponse> GetMyNotificationsAsync(int take, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task PublishToUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        string type,
        string title,
        string message,
        string? entityName,
        string? entityId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);
}
