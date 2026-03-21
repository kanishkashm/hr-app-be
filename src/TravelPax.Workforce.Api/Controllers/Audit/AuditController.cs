using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Audit;
using TravelPax.Workforce.Contracts.Audit;
using TravelPax.Workforce.Domain.Constants;

namespace TravelPax.Workforce.Api.Controllers.Audit;

[ApiController]
[Route("api/v1/audit")]
[Authorize]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet("logs")]
    [Authorize(Policy = PermissionCodes.AuditView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var response = await auditService.GetAuditLogsAsync(take, cancellationToken);
        return Ok(response);
    }

    [HttpGet("login-attempts")]
    [Authorize(Policy = PermissionCodes.AuditView)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LoginAuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoginAuditLogs([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var response = await auditService.GetLoginAuditLogsAsync(take, cancellationToken);
        return Ok(response);
    }
}
