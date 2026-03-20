using TravelPax.Workforce.Contracts.Audit;

namespace TravelPax.Workforce.Application.Abstractions.Audit;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditLogResponse>> GetAuditLogsAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoginAuditLogResponse>> GetLoginAuditLogsAsync(int take, CancellationToken cancellationToken = default);
}
