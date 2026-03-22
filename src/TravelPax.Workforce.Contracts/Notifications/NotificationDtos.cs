namespace TravelPax.Workforce.Contracts.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? EntityName,
    string? EntityId,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationSummaryResponse(
    int UnreadCount,
    IReadOnlyCollection<NotificationResponse> Items);
