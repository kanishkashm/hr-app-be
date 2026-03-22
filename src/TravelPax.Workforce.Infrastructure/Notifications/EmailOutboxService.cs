using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.Notifications;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Notifications;

public sealed class EmailOutboxService(TravelPaxDbContext dbContext) : IEmailOutboxService
{
    public async Task QueueAsync(
        IReadOnlyCollection<string> toEmails,
        string subject,
        string bodyText,
        string? bodyHtml,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var recipients = toEmails
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (recipients.Length == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var messages = recipients.Select(email => new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            ToEmail = email,
            Subject = subject,
            BodyText = bodyText,
            BodyHtml = bodyHtml,
            Status = "Pending",
            AttemptCount = 0,
            NextAttemptAt = now,
            CreatedAt = now,
            CreatedBy = actorUserId,
            UpdatedAt = now,
            UpdatedBy = actorUserId
        });

        await dbContext.EmailOutboxMessages.AddRangeAsync(messages, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
