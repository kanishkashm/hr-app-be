using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.Audit;
using TravelPax.Workforce.Contracts.Audit;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Audit;

public sealed class AuditService(TravelPaxDbContext dbContext) : IAuditService
{
    public async Task<IReadOnlyCollection<AuditLogResponse>> GetAuditLogsAsync(int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        var users = dbContext.Users.AsQueryable();
        var items = await (
            from audit in dbContext.AuditLogs
            join actor in users on audit.ActorUserId equals actor.Id into actorJoin
            from actor in actorJoin.DefaultIfEmpty()
            orderby audit.OccurredAt descending
            select new AuditLogResponse(
                audit.Id,
                audit.OccurredAt,
                audit.Module,
                audit.Action,
                audit.EntityName,
                audit.EntityId,
                actor != null ? actor.DisplayName : "System",
                audit.IpAddress,
                audit.OldValues,
                audit.NewValues))
            .Take(take)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyCollection<LoginAuditLogResponse>> GetLoginAuditLogsAsync(int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        return await dbContext.LoginAuditLogs
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .Select(x => new LoginAuditLogResponse(
                x.Id,
                x.OccurredAt,
                x.EmailOrUsername,
                x.Status,
                x.FailureReason,
                x.IpAddress,
                x.DeviceSummary,
                x.LogoutAt))
            .ToListAsync(cancellationToken);
    }
}
