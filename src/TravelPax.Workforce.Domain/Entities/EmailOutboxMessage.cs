using TravelPax.Workforce.Domain.Common;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class EmailOutboxMessage : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
