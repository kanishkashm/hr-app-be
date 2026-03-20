using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPax.Workforce.Application.Abstractions.Settings;
using TravelPax.Workforce.Contracts.Settings;

namespace TravelPax.Workforce.Api.Controllers.Settings;

[ApiController]
[Route("api/v1/settings")]
[Authorize]
public sealed class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(SettingsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var response = await settingsService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("company")]
    [ProducesResponseType(typeof(CompanySettingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanySettingRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateCompanyAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("branches")]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBranch([FromBody] UpsertBranchRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateBranchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOverview), new { }, response);
    }

    [HttpPut("branches/{branchId:guid}")]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBranch(Guid branchId, [FromBody] UpsertBranchRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateBranchAsync(branchId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("allowed-networks")]
    [ProducesResponseType(typeof(AllowedNetworkResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAllowedNetwork([FromBody] UpsertAllowedNetworkRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateAllowedNetworkAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOverview), new { }, response);
    }

    [HttpPut("allowed-networks/{networkId:guid}")]
    [ProducesResponseType(typeof(AllowedNetworkResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAllowedNetwork(Guid networkId, [FromBody] UpsertAllowedNetworkRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateAllowedNetworkAsync(networkId, request, cancellationToken);
        return Ok(response);
    }
}
