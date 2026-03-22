namespace TravelPax.Workforce.Application.Abstractions.Notifications;

public interface IEmailOutboxService
{
    Task QueueAsync(
        IReadOnlyCollection<string> toEmails,
        string subject,
        string bodyText,
        string? bodyHtml,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);
}
