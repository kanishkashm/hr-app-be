using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Notifications;
using TravelPax.Workforce.Contracts.Notifications;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Notifications;

public sealed class NotificationService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService) : INotificationService
{
    public async Task<NotificationSummaryResponse> GetMyNotificationsAsync(int take, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        take = Math.Clamp(take, 1, 100);
        var userId = currentUserService.UserId.Value;

        var unreadCount = await dbContext.UserNotifications
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

        var items = await dbContext.UserNotifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new NotificationResponse(
                x.Id,
                x.Type,
                x.Title,
                x.Message,
                x.EntityName,
                x.EntityId,
                x.IsRead,
                x.CreatedAt,
                x.ReadAt))
            .ToListAsync(cancellationToken);

        return new NotificationSummaryResponse(unreadCount, items);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userId = currentUserService.UserId.Value;
        var entity = await dbContext.UserNotifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found.");

        if (entity.IsRead)
        {
            return;
        }

        entity.IsRead = true;
        entity.ReadAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = userId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userId = currentUserService.UserId.Value;
        var unreadItems = await dbContext.UserNotifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadItems.Count == 0)
        {
            return;
        }

        foreach (var item in unreadItems)
        {
            item.IsRead = true;
            item.ReadAt = DateTimeOffset.UtcNow;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.UpdatedBy = userId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishToUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        string type,
        string title,
        string message,
        string? entityName,
        string? entityId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var notifications = userIds.Distinct().Select(userId => new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            EntityName = entityName,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = now,
            CreatedBy = actorUserId,
            UpdatedAt = now,
            UpdatedBy = actorUserId
        });

        await dbContext.UserNotifications.AddRangeAsync(notifications, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
