namespace TravelPax.Workforce.Contracts.Audit;

public sealed record AuditLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Module,
    string Action,
    string EntityName,
    string EntityId,
    string ActorDisplayName,
    string? IpAddress,
    string? OldValues,
    string? NewValues);

public sealed record LoginAuditLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? EmailOrUsername,
    string Status,
    string? FailureReason,
    string? IpAddress,
    string? DeviceSummary,
    DateTimeOffset? LogoutAt);
