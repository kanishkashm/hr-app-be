using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Settings;
using TravelPax.Workforce.Contracts.Settings;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Settings;

public sealed class SettingsService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService) : ISettingsService
{
    public async Task<SettingsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyEntityAsync(cancellationToken);
        var branches = await dbContext.OfficeBranches
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var networks = await dbContext.AllowedNetworks
            .Include(x => x.Branch)
            .OrderBy(x => x.Branch.Name)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new SettingsOverviewResponse(
            MapCompany(company),
            branches.Select(MapBranch).ToArray(),
            networks.Select(MapNetwork).ToArray());
    }

    public async Task<CompanySettingResponse> UpdateCompanyAsync(UpdateCompanySettingRequest request, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyEntityAsync(cancellationToken);
        var actorId = currentUserService.UserId;

        var oldValues = $"CompanyName={company.CompanyName};Timezone={company.DefaultTimezone};Start={company.WorkingDayStartTime};End={company.WorkingDayEndTime};LateGrace={company.LateGraceMinutes}";

        company.CompanyName = request.CompanyName.Trim();
        company.DefaultTimezone = request.DefaultTimezone.Trim();
        company.WorkingDayStartTime = ParseTime(request.WorkingDayStartTime, new TimeOnly(9, 0));
        company.WorkingDayEndTime = ParseTime(request.WorkingDayEndTime, new TimeOnly(18, 0));
        company.LateGraceMinutes = Math.Max(request.LateGraceMinutes, 0);
        company.WeekendConfig = string.IsNullOrWhiteSpace(request.WeekendConfig) ? company.WeekendConfig : request.WeekendConfig.Trim();
        company.UpdatedAt = DateTimeOffset.UtcNow;
        company.UpdatedBy = actorId;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = "CompanySettingsUpdated",
            Module = "Settings",
            EntityName = nameof(CompanySetting),
            EntityId = company.Id.ToString(),
            OldValues = oldValues,
            NewValues = $"CompanyName={company.CompanyName};Timezone={company.DefaultTimezone};Start={company.WorkingDayStartTime};End={company.WorkingDayEndTime};LateGrace={company.LateGraceMinutes}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCompany(company);
    }

    public async Task<BranchResponse> CreateBranchAsync(UpsertBranchRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var branch = new OfficeBranch
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            City = NormalizeNullable(request.City),
            Country = string.IsNullOrWhiteSpace(request.Country) ? "Sri Lanka" : request.Country.Trim(),
            Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? "Asia/Colombo" : request.Timezone.Trim(),
            IsActive = request.IsActive,
            CreatedBy = actorId,
            UpdatedBy = actorId
        };

        dbContext.OfficeBranches.Add(branch);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = "BranchCreated",
            Module = "Settings",
            EntityName = nameof(OfficeBranch),
            EntityId = branch.Id.ToString(),
            NewValues = $"Code={branch.Code};Name={branch.Name};Timezone={branch.Timezone};IsActive={branch.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapBranch(branch);
    }

    public async Task<BranchResponse> UpdateBranchAsync(Guid branchId, UpsertBranchRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == branchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");

        var oldValues = $"Code={branch.Code};Name={branch.Name};Timezone={branch.Timezone};IsActive={branch.IsActive}";

        branch.Code = request.Code.Trim().ToUpperInvariant();
        branch.Name = request.Name.Trim();
        branch.City = NormalizeNullable(request.City);
        branch.Country = string.IsNullOrWhiteSpace(request.Country) ? "Sri Lanka" : request.Country.Trim();
        branch.Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? "Asia/Colombo" : request.Timezone.Trim();
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTimeOffset.UtcNow;
        branch.UpdatedBy = actorId;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = "BranchUpdated",
            Module = "Settings",
            EntityName = nameof(OfficeBranch),
            EntityId = branch.Id.ToString(),
            OldValues = oldValues,
            NewValues = $"Code={branch.Code};Name={branch.Name};Timezone={branch.Timezone};IsActive={branch.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapBranch(branch);
    }

    public async Task<AllowedNetworkResponse> CreateAllowedNetworkAsync(UpsertAllowedNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");

        var network = new AllowedNetwork
        {
            Id = Guid.NewGuid(),
            BranchId = branch.Id,
            Name = request.Name.Trim(),
            NetworkType = request.NetworkType.Trim(),
            IpOrCidr = request.IpOrCidr.Trim(),
            ValidationMode = request.ValidationMode.Trim(),
            IsActive = request.IsActive,
            Priority = request.Priority,
            CreatedBy = actorId,
            UpdatedBy = actorId
        };

        dbContext.AllowedNetworks.Add(network);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = "AllowedNetworkCreated",
            Module = "Settings",
            EntityName = nameof(AllowedNetwork),
            EntityId = network.Id.ToString(),
            NewValues = $"Branch={branch.Code};Name={network.Name};IpOrCidr={network.IpOrCidr};Type={network.NetworkType};Active={network.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        network.Branch = branch;
        return MapNetwork(network);
    }

    public async Task<AllowedNetworkResponse> UpdateAllowedNetworkAsync(Guid networkId, UpsertAllowedNetworkRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var network = await dbContext.AllowedNetworks
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == networkId, cancellationToken)
            ?? throw new InvalidOperationException("Allowed network not found.");

        var branch = network.BranchId == request.BranchId
            ? network.Branch
            : await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == request.BranchId, cancellationToken)
                ?? throw new InvalidOperationException("Branch not found.");

        var oldValues = $"Branch={network.Branch.Code};Name={network.Name};IpOrCidr={network.IpOrCidr};Type={network.NetworkType};Active={network.IsActive}";

        network.BranchId = branch.Id;
        network.Branch = branch;
        network.Name = request.Name.Trim();
        network.NetworkType = request.NetworkType.Trim();
        network.IpOrCidr = request.IpOrCidr.Trim();
        network.ValidationMode = request.ValidationMode.Trim();
        network.IsActive = request.IsActive;
        network.Priority = request.Priority;
        network.UpdatedAt = DateTimeOffset.UtcNow;
        network.UpdatedBy = actorId;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = "AllowedNetworkUpdated",
            Module = "Settings",
            EntityName = nameof(AllowedNetwork),
            EntityId = network.Id.ToString(),
            OldValues = oldValues,
            NewValues = $"Branch={branch.Code};Name={network.Name};IpOrCidr={network.IpOrCidr};Type={network.NetworkType};Active={network.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapNetwork(network);
    }

    private async Task<CompanySetting> GetCompanyEntityAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
    }

    private static CompanySettingResponse MapCompany(CompanySetting company)
    {
        return new CompanySettingResponse(
            company.Id,
            company.CompanyName,
            company.DefaultTimezone,
            company.WorkingDayStartTime.ToString("HH:mm"),
            company.WorkingDayEndTime.ToString("HH:mm"),
            company.LateGraceMinutes,
            company.WeekendConfig);
    }

    private static BranchResponse MapBranch(OfficeBranch branch)
    {
        return new BranchResponse(branch.Id, branch.Code, branch.Name, branch.City, branch.Country, branch.Timezone, branch.IsActive);
    }

    private static AllowedNetworkResponse MapNetwork(AllowedNetwork network)
    {
        return new AllowedNetworkResponse(
            network.Id,
            network.BranchId,
            network.Branch.Name,
            network.Name,
            network.NetworkType,
            network.IpOrCidr,
            network.ValidationMode,
            network.IsActive,
            network.Priority);
    }

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TimeOnly ParseTime(string value, TimeOnly fallback)
    {
        return TimeOnly.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
