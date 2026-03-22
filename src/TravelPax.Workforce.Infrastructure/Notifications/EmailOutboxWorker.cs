using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Notifications;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> emailOptions,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Email outbox worker failed while processing batch.");
            }

            var waitSeconds = Math.Clamp(emailOptions.Value.PollIntervalSeconds, 5, 300);
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var options = emailOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.FromEmail))
        {
            logger.LogWarning("Email outbox is enabled but Email.Host or Email.FromEmail is missing.");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TravelPaxDbContext>();
        var now = DateTimeOffset.UtcNow;
        var maxAttempts = Math.Max(1, options.MaxAttempts);
        var batchSize = Math.Clamp(options.BatchSize, 1, 200);

        var pendingMessages = await dbContext.EmailOutboxMessages
            .Where(x => x.Status == "Pending"
                        && x.AttemptCount < maxAttempts
                        && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        using var smtp = BuildSmtpClient(options);
        foreach (var message in pendingMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TrySendAsync(smtp, options, message, now, maxAttempts, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SmtpClient BuildSmtpClient(EmailOptions options)
    {
        var smtp = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            smtp.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        return smtp;
    }

    private async Task TrySendAsync(
        SmtpClient smtp,
        EmailOptions options,
        EmailOutboxMessage entity,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(options.FromEmail, options.FromName),
                Subject = entity.Subject,
                Body = string.IsNullOrWhiteSpace(entity.BodyHtml) ? entity.BodyText : entity.BodyHtml,
                IsBodyHtml = !string.IsNullOrWhiteSpace(entity.BodyHtml)
            };

            mail.To.Add(entity.ToEmail);
            await smtp.SendMailAsync(mail, cancellationToken);

            entity.Status = "Sent";
            entity.SentAt = DateTimeOffset.UtcNow;
            entity.LastAttemptAt = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.LastError = null;
        }
        catch (Exception exception)
        {
            entity.AttemptCount += 1;
            entity.LastAttemptAt = DateTimeOffset.UtcNow;
            entity.LastError = exception.Message.Length > 1900 ? exception.Message[..1900] : exception.Message;
            entity.NextAttemptAt = now.AddMinutes(Math.Min(30, Math.Max(1, entity.AttemptCount * 2)));
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.Status = entity.AttemptCount >= maxAttempts ? "Failed" : "Pending";

            logger.LogWarning(exception, "Failed to send outbox email {EmailOutboxId} to {ToEmail}. Attempt {Attempt}", entity.Id, entity.ToEmail, entity.AttemptCount);
        }
    }
}
