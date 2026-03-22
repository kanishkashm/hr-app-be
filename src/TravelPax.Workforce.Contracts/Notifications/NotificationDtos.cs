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

public sealed record EmailOutboxHealthResponse(
    int PendingCount,
    int SentCount,
    int FailedCount,
    DateTimeOffset? OldestPendingCreatedAt,
    DateTimeOffset? LastSentAt,
    DateTimeOffset? LastFailedAt,
    IReadOnlyCollection<EmailOutboxFailureSampleResponse> RecentFailures);

public sealed record EmailOutboxFailureSampleResponse(
    Guid Id,
    string ToEmail,
    string Subject,
    int AttemptCount,
    DateTimeOffset? LastAttemptAt,
    string? LastError);
