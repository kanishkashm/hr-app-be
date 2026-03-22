using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Application.Abstractions.Settings;
using TravelPax.Workforce.Contracts.Settings;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Settings;

public sealed class SettingsService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService,
    INetworkValidationService networkValidationService) : ISettingsService
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

    public async Task<NetworkValidationCheckResponse> TestNetworkAsync(NetworkValidationCheckRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");
        var ipAddress = request.IpAddress?.Trim();

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new InvalidOperationException("IP address is required.");
        }

        var result = await networkValidationService.ValidateAsync(branch.Id, ipAddress, cancellationToken);
        var matchedRule = result.MatchedRuleId is null
            ? null
            : await dbContext.AllowedNetworks.FirstOrDefaultAsync(x => x.Id == result.MatchedRuleId.Value, cancellationToken);

        return new NetworkValidationCheckResponse(
            branch.Id,
            ipAddress,
            result.Status,
            matchedRule?.Name,
            matchedRule?.IpOrCidr);
    }

    public async Task<IReadOnlyCollection<AttendancePeriodLockResponse>> GetAttendancePeriodLocksAsync(
        int? year,
        int? month,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var resolvedYear = year ?? now.Year;

        var query = dbContext.AttendancePeriodLocks
            .Include(x => x.Branch)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ThenBy(x => x.BranchId == null ? 0 : 1)
            .AsQueryable();

        query = query.Where(x => x.Year == resolvedYear);
        if (month is not null)
        {
            query = query.Where(x => x.Month == month.Value);
        }

        if (branchId is not null)
        {
            query = query.Where(x => x.BranchId == branchId);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items.Select(MapAttendancePeriodLock).ToArray();
    }

    public async Task<AttendancePeriodLockResponse> SetAttendancePeriodLockAsync(
        SetAttendancePeriodLockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Year < 2000 || request.Year > 2100)
        {
            throw new InvalidOperationException("Year must be between 2000 and 2100.");
        }

        if (request.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Month must be between 1 and 12.");
        }

        var actorId = currentUserService.UserId;
        var actor = actorId is null
            ? throw new UnauthorizedAccessException("User is not authenticated.")
            : actorId.Value;

        OfficeBranch? branch = null;
        if (request.BranchId is not null)
        {
            branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == request.BranchId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Branch not found.");
        }

        var entity = await dbContext.AttendancePeriodLocks
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(
                x => x.Year == request.Year
                     && x.Month == request.Month
                     && x.BranchId == request.BranchId,
                cancellationToken);

        var action = request.IsLocked ? "AttendancePeriodLocked" : "AttendancePeriodUnlocked";
        var scope = branch?.Code ?? "ALL_BRANCHES";
        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        if (!request.IsLocked)
        {
            var isFinalized = await dbContext.PayrollPeriodFinalizations.AnyAsync(
                x => x.IsFinalized
                     && x.Year == request.Year
                     && x.Month == request.Month
                     && (x.BranchId == request.BranchId || x.BranchId == null || request.BranchId == null),
                cancellationToken);
            if (isFinalized)
            {
                throw new InvalidOperationException("This period is already finalized for payroll. Re-open workflow is required before unlocking.");
            }
        }

        if (entity is null)
        {
            entity = new AttendancePeriodLock
            {
                Id = Guid.NewGuid(),
                Year = request.Year,
                Month = request.Month,
                BranchId = request.BranchId,
                Branch = branch,
                IsLocked = request.IsLocked,
                LockedAt = DateTimeOffset.UtcNow,
                LockedByUserId = request.IsLocked ? actor : null,
                UnlockedAt = request.IsLocked ? null : DateTimeOffset.UtcNow,
                UnlockedByUserId = request.IsLocked ? null : actor,
                Notes = normalizedNotes,
                CreatedBy = actor,
                UpdatedBy = actor
            };

            dbContext.AttendancePeriodLocks.Add(entity);
        }
        else
        {
            entity.IsLocked = request.IsLocked;
            entity.Notes = normalizedNotes;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = actor;

            if (request.IsLocked)
            {
                entity.LockedAt = DateTimeOffset.UtcNow;
                entity.LockedByUserId = actor;
                entity.UnlockedAt = null;
                entity.UnlockedByUserId = null;
            }
            else
            {
                entity.UnlockedAt = DateTimeOffset.UtcNow;
                entity.UnlockedByUserId = actor;
            }
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = action,
            Module = "Attendance",
            EntityName = nameof(AttendancePeriodLock),
            EntityId = entity.Id.ToString(),
            NewValues = $"Year={request.Year};Month={request.Month};Scope={scope};IsLocked={request.IsLocked};Notes={entity.Notes}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        if (entity.Branch is null && entity.BranchId is not null)
        {
            await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        }

        return MapAttendancePeriodLock(entity);
    }

    public async Task<IReadOnlyCollection<PayrollPeriodFinalizationResponse>> GetPayrollFinalizationsAsync(
        int? year,
        int? month,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var resolvedYear = year ?? now.Year;

        var query = dbContext.PayrollPeriodFinalizations
            .Include(x => x.Branch)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ThenBy(x => x.BranchId == null ? 0 : 1)
            .AsQueryable();

        query = query.Where(x => x.Year == resolvedYear);
        if (month is not null)
        {
            query = query.Where(x => x.Month == month.Value);
        }

        if (branchId is not null)
        {
            query = query.Where(x => x.BranchId == branchId);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items.Select(MapPayrollFinalization).ToArray();
    }

    public async Task<PayrollPeriodFinalizationResponse> FinalizePayrollPeriodAsync(
        FinalizePayrollPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Year < 2000 || request.Year > 2100)
        {
            throw new InvalidOperationException("Year must be between 2000 and 2100.");
        }

        if (request.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Month must be between 1 and 12.");
        }

        var actorId = currentUserService.UserId;
        var actor = actorId is null
            ? throw new UnauthorizedAccessException("User is not authenticated.")
            : actorId.Value;

        OfficeBranch? branch = null;
        if (request.BranchId is not null)
        {
            branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == request.BranchId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Branch not found.");
        }

        var alreadyFinalized = await dbContext.PayrollPeriodFinalizations.AnyAsync(
            x => x.IsFinalized
                 && x.Year == request.Year
                 && x.Month == request.Month
                 && x.BranchId == request.BranchId,
            cancellationToken);
        if (alreadyFinalized)
        {
            throw new InvalidOperationException("This payroll period is already finalized for the selected scope.");
        }

        var isLocked = await dbContext.AttendancePeriodLocks.AnyAsync(
            x => x.IsLocked
                 && x.Year == request.Year
                 && x.Month == request.Month
                 && (x.BranchId == request.BranchId || x.BranchId == null),
            cancellationToken);
        if (!isLocked)
        {
            throw new InvalidOperationException("Lock the attendance period before finalizing payroll.");
        }

        var periodStart = new DateOnly(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var attendanceQuery = dbContext.AttendanceRecords
            .Include(x => x.User)
            .Where(x => x.AttendanceDate >= periodStart && x.AttendanceDate <= periodEnd);
        if (request.BranchId is not null)
        {
            attendanceQuery = attendanceQuery.Where(x => x.BranchId == request.BranchId.Value);
        }

        var attendance = await attendanceQuery.ToListAsync(cancellationToken);
        var activeUsersQuery = dbContext.Users.AsQueryable();
        if (request.BranchId is not null)
        {
            activeUsersQuery = activeUsersQuery.Where(x => x.BranchId == request.BranchId.Value);
        }
        var activeUsers = await activeUsersQuery.CountAsync(x => x.Status == "Active", cancellationToken);

        var summary = new
        {
            periodStart = periodStart.ToString("yyyy-MM-dd"),
            periodEnd = periodEnd.ToString("yyyy-MM-dd"),
            activeUsers,
            attendanceRecords = attendance.Count,
            uniqueEmployees = attendance.Select(x => x.UserId).Distinct().Count(),
            presentDays = attendance.Count(x => x.Status == "Present"),
            lateDays = attendance.Count(x => x.Status == "Late"),
            pendingClockOutDays = attendance.Count(x => x.Status == "PendingClockOut" || x.ClockInAt == null || x.ClockOutAt == null),
            totalWorkHours = Math.Round(attendance.Where(x => x.TotalWorkMinutes.HasValue).Sum(x => x.TotalWorkMinutes!.Value) / 60d, 2)
        };

        var entity = new PayrollPeriodFinalization
        {
            Id = Guid.NewGuid(),
            Year = request.Year,
            Month = request.Month,
            BranchId = request.BranchId,
            Branch = branch,
            IsFinalized = true,
            FinalizedAt = DateTimeOffset.UtcNow,
            FinalizedByUserId = actor,
            SnapshotJson = JsonSerializer.Serialize(summary),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedBy = actor,
            UpdatedBy = actor
        };

        dbContext.PayrollPeriodFinalizations.Add(entity);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = "PayrollPeriodFinalized",
            Module = "Payroll",
            EntityName = nameof(PayrollPeriodFinalization),
            EntityId = entity.Id.ToString(),
            NewValues = $"Year={request.Year};Month={request.Month};Scope={(branch?.Code ?? "ALL_BRANCHES")};IsFinalized=true"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        if (entity.Branch is null && entity.BranchId is not null)
        {
            await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        }

        return MapPayrollFinalization(entity);
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

    private static AttendancePeriodLockResponse MapAttendancePeriodLock(AttendancePeriodLock item)
    {
        return new AttendancePeriodLockResponse(
            item.Id,
            item.Year,
            item.Month,
            item.BranchId,
            item.Branch?.Name ?? "All Branches",
            item.IsLocked,
            item.LockedAt,
            item.LockedByUserId,
            item.UnlockedAt,
            item.UnlockedByUserId,
            item.Notes);
    }

    private static PayrollPeriodFinalizationResponse MapPayrollFinalization(PayrollPeriodFinalization item)
    {
        return new PayrollPeriodFinalizationResponse(
            item.Id,
            item.Year,
            item.Month,
            item.BranchId,
            item.Branch?.Name ?? "All Branches",
            item.IsFinalized,
            item.FinalizedAt,
            item.FinalizedByUserId,
            item.ReopenedAt,
            item.ReopenedByUserId,
            item.SnapshotJson,
            item.Notes);
    }

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TimeOnly ParseTime(string value, TimeOnly fallback)
    {
        return TimeOnly.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
