using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Application.Abstractions.Settings;
using TravelPax.Workforce.Contracts.Settings;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Attendance;
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

    public async Task<PayrollPeriodFinalizationResponse> ReopenPayrollPeriodAsync(
        Guid finalizationId,
        ReopenPayrollPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Re-open reason is required.");
        }

        var actorId = currentUserService.UserId;
        var actor = actorId is null
            ? throw new UnauthorizedAccessException("User is not authenticated.")
            : actorId.Value;

        var canReopen = await dbContext.UserRoles
            .Where(x => x.UserId == actor)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (_, role) => role.Name)
            .AnyAsync(role => role == RoleCodes.SuperAdmin || role == RoleCodes.HrAdmin, cancellationToken);
        if (!canReopen)
        {
            throw new InvalidOperationException("Only Super Admin or HR Admin can reopen finalized payroll periods.");
        }

        var entity = await dbContext.PayrollPeriodFinalizations
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == finalizationId, cancellationToken)
            ?? throw new InvalidOperationException("Payroll finalization not found.");

        if (!entity.IsFinalized)
        {
            throw new InvalidOperationException("Payroll period is already reopened.");
        }

        entity.IsFinalized = false;
        entity.ReopenedAt = DateTimeOffset.UtcNow;
        entity.ReopenedByUserId = actor;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actor;
        var reason = request.Reason.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(entity.Notes)
            ? $"Reopened: {reason}"
            : $"{entity.Notes}\nReopened: {reason}";

        if (request.UnlockAttendanceLock)
        {
            var lockEntity = await dbContext.AttendancePeriodLocks
                .FirstOrDefaultAsync(
                    x => x.Year == entity.Year
                         && x.Month == entity.Month
                         && x.BranchId == entity.BranchId,
                    cancellationToken);
            if (lockEntity is not null && lockEntity.IsLocked)
            {
                lockEntity.IsLocked = false;
                lockEntity.UnlockedAt = DateTimeOffset.UtcNow;
                lockEntity.UnlockedByUserId = actor;
                lockEntity.UpdatedAt = DateTimeOffset.UtcNow;
                lockEntity.UpdatedBy = actor;
                lockEntity.Notes = string.IsNullOrWhiteSpace(lockEntity.Notes)
                    ? $"Unlocked via payroll reopen: {reason}"
                    : $"{lockEntity.Notes}\nUnlocked via payroll reopen: {reason}";
            }
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = "PayrollPeriodReopened",
            Module = "Payroll",
            EntityName = nameof(PayrollPeriodFinalization),
            EntityId = entity.Id.ToString(),
            NewValues = $"Year={entity.Year};Month={entity.Month};Scope={(entity.Branch?.Code ?? "ALL_BRANCHES")};Reason={reason};UnlockAttendanceLock={request.UnlockAttendanceLock}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPayrollFinalization(entity);
    }

    public async Task<IReadOnlyCollection<AttendanceRuleProfileResponse>> GetAttendanceRuleProfilesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await dbContext.AttendanceRuleProfiles
            .Include(x => x.Branch)
            .Include(x => x.Shift)
            .OrderBy(x => x.ScopeType == "Shift" ? 0 : x.ScopeType == "Branch" ? 1 : 2)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rules.Select(MapAttendanceRuleProfile).ToArray();
    }

    public async Task<AttendanceRuleProfileResponse> CreateAttendanceRuleProfileAsync(
        UpsertAttendanceRuleProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentUserService.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var normalizedScope = NormalizeScope(request.ScopeType);
        var (branch, shift) = await ValidateScopeReferencesAsync(normalizedScope, request.BranchId, request.ShiftId, cancellationToken);

        var entity = new AttendanceRuleProfile
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ScopeType = normalizedScope,
            BranchId = branch?.Id,
            ShiftId = shift?.Id,
            Priority = Math.Clamp(request.Priority, 1, 9999),
            IsActive = request.IsActive,
            LateGraceMinutes = NormalizeNullableNonNegative(request.LateGraceMinutes),
            HalfDayThresholdMinutes = NormalizeNullableNonNegative(request.HalfDayThresholdMinutes),
            MinPresentMinutes = NormalizeNullableNonNegative(request.MinPresentMinutes),
            OvertimeThresholdMinutes = NormalizeNullableNonNegative(request.OvertimeThresholdMinutes),
            EarlyOutGraceMinutes = NormalizeNullableNonNegative(request.EarlyOutGraceMinutes),
            ShortLeaveDeductionMinutes = NormalizeNullableNonNegative(request.ShortLeaveDeductionMinutes),
            EnableMissedPunchDetection = request.EnableMissedPunchDetection,
            CreatedBy = actor,
            UpdatedBy = actor,
            Branch = branch,
            Shift = shift
        };

        dbContext.AttendanceRuleProfiles.Add(entity);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = "AttendanceRuleProfileCreated",
            Module = "Attendance",
            EntityName = nameof(AttendanceRuleProfile),
            EntityId = entity.Id.ToString(),
            NewValues = $"Name={entity.Name};Scope={entity.ScopeType};BranchId={entity.BranchId};ShiftId={entity.ShiftId};Priority={entity.Priority};Active={entity.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAttendanceRuleProfile(entity);
    }

    public async Task<AttendanceRuleProfileResponse> UpdateAttendanceRuleProfileAsync(
        Guid ruleId,
        UpsertAttendanceRuleProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentUserService.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var entity = await dbContext.AttendanceRuleProfiles
            .Include(x => x.Branch)
            .Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.Id == ruleId, cancellationToken)
            ?? throw new InvalidOperationException("Attendance rule profile not found.");

        var normalizedScope = NormalizeScope(request.ScopeType);
        var (branch, shift) = await ValidateScopeReferencesAsync(normalizedScope, request.BranchId, request.ShiftId, cancellationToken);
        var oldValues = $"Name={entity.Name};Scope={entity.ScopeType};BranchId={entity.BranchId};ShiftId={entity.ShiftId};Priority={entity.Priority};Active={entity.IsActive}";

        entity.Name = request.Name.Trim();
        entity.ScopeType = normalizedScope;
        entity.BranchId = branch?.Id;
        entity.ShiftId = shift?.Id;
        entity.Priority = Math.Clamp(request.Priority, 1, 9999);
        entity.IsActive = request.IsActive;
        entity.LateGraceMinutes = NormalizeNullableNonNegative(request.LateGraceMinutes);
        entity.HalfDayThresholdMinutes = NormalizeNullableNonNegative(request.HalfDayThresholdMinutes);
        entity.MinPresentMinutes = NormalizeNullableNonNegative(request.MinPresentMinutes);
        entity.OvertimeThresholdMinutes = NormalizeNullableNonNegative(request.OvertimeThresholdMinutes);
        entity.EarlyOutGraceMinutes = NormalizeNullableNonNegative(request.EarlyOutGraceMinutes);
        entity.ShortLeaveDeductionMinutes = NormalizeNullableNonNegative(request.ShortLeaveDeductionMinutes);
        entity.EnableMissedPunchDetection = request.EnableMissedPunchDetection;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actor;
        entity.Branch = branch;
        entity.Shift = shift;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = "AttendanceRuleProfileUpdated",
            Module = "Attendance",
            EntityName = nameof(AttendanceRuleProfile),
            EntityId = entity.Id.ToString(),
            OldValues = oldValues,
            NewValues = $"Name={entity.Name};Scope={entity.ScopeType};BranchId={entity.BranchId};ShiftId={entity.ShiftId};Priority={entity.Priority};Active={entity.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAttendanceRuleProfile(entity);
    }

    public async Task<AttendanceRulePreviewResponse> PreviewAttendanceRuleAsync(
        AttendanceRulePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyEntityAsync(cancellationToken);

        ShiftDefinition? shift = null;
        if (request.ShiftId is not null)
        {
            shift = await dbContext.ShiftDefinitions.FirstOrDefaultAsync(x => x.Id == request.ShiftId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Shift not found.");
        }

        var profiles = await dbContext.AttendanceRuleProfiles
            .Include(x => x.Branch)
            .Include(x => x.Shift)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var profile = AttendanceRulesEngine.ResolveBestProfile(profiles, request.BranchId, request.ShiftId);
        var rules = AttendanceRulesEngine.BuildEffectiveRules(company, shift, profile);
        var businessDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, company.DefaultTimezone ?? AttendanceRulesEngine.DefaultTimezone));
        var computed = AttendanceRulesEngine.Evaluate(
            request.AttendanceDate,
            request.ClockInAt,
            request.ClockOutAt,
            company,
            shift,
            rules,
            businessDate);

        return new AttendanceRulePreviewResponse(
            computed.SuggestedStatus,
            computed.TotalWorkMinutes,
            computed.IsLate,
            computed.LateMinutes,
            computed.IsEarlyOut,
            computed.EarlyOutMinutes,
            computed.IsOvertime,
            computed.OvertimeMinutes,
            computed.IsMissedPunch,
            profile?.Id,
            profile?.Name ?? "Default Company/Shift Rules",
            profile?.ScopeType ?? "SystemDefault");
    }

    public async Task<IReadOnlyCollection<WorkCalendarEntryResponse>> GetWorkCalendarEntriesAsync(
        int year,
        int month,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        ValidateYearMonth(year, month);

        if (branchId is not null)
        {
            var branchExists = await dbContext.OfficeBranches.AnyAsync(x => x.Id == branchId.Value, cancellationToken);
            if (!branchExists)
            {
                throw new InvalidOperationException("Branch not found.");
            }
        }

        var query = dbContext.WorkCalendarEntries
            .Include(x => x.Branch)
            .AsQueryable();

        if (branchId is not null)
        {
            query = query.Where(x => x.BranchId == null || x.BranchId == branchId.Value);
        }

        query = query.Where(x =>
            (!x.IsRecurringAnnual && x.CalendarDate.Year == year && x.CalendarDate.Month == month)
            || (x.IsRecurringAnnual && x.CalendarDate.Month == month));

        var items = await query
            .OrderBy(x => x.CalendarDate.Month)
            .ThenBy(x => x.CalendarDate.Day)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items.Select(MapWorkCalendarEntry).ToArray();
    }

    public async Task<WorkCalendarEntryResponse> CreateWorkCalendarEntryAsync(
        UpsertWorkCalendarEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentUserService.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var (branch, normalizedDayType, normalizedName, normalizedNotes) = await ValidateWorkCalendarInputAsync(request, cancellationToken);

        var entity = new WorkCalendarEntry
        {
            Id = Guid.NewGuid(),
            BranchId = branch?.Id,
            Branch = branch,
            CalendarDate = request.CalendarDate,
            DayType = normalizedDayType,
            Name = normalizedName,
            IsRecurringAnnual = request.IsRecurringAnnual,
            IsActive = request.IsActive,
            Notes = normalizedNotes,
            CreatedBy = actor,
            UpdatedBy = actor
        };

        dbContext.WorkCalendarEntries.Add(entity);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = "WorkCalendarEntryCreated",
            Module = "Settings",
            EntityName = nameof(WorkCalendarEntry),
            EntityId = entity.Id.ToString(),
            NewValues = $"BranchId={entity.BranchId};Date={entity.CalendarDate};Type={entity.DayType};Name={entity.Name};Recurring={entity.IsRecurringAnnual};Active={entity.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapWorkCalendarEntry(entity);
    }

    public async Task<WorkCalendarEntryResponse> UpdateWorkCalendarEntryAsync(
        Guid entryId,
        UpsertWorkCalendarEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentUserService.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var entity = await dbContext.WorkCalendarEntries
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == entryId, cancellationToken)
            ?? throw new InvalidOperationException("Work calendar entry not found.");

        var (branch, normalizedDayType, normalizedName, normalizedNotes) = await ValidateWorkCalendarInputAsync(request, cancellationToken);
        var oldValues = $"BranchId={entity.BranchId};Date={entity.CalendarDate};Type={entity.DayType};Name={entity.Name};Recurring={entity.IsRecurringAnnual};Active={entity.IsActive}";

        entity.BranchId = branch?.Id;
        entity.Branch = branch;
        entity.CalendarDate = request.CalendarDate;
        entity.DayType = normalizedDayType;
        entity.Name = normalizedName;
        entity.IsRecurringAnnual = request.IsRecurringAnnual;
        entity.IsActive = request.IsActive;
        entity.Notes = normalizedNotes;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actor;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor,
            Action = "WorkCalendarEntryUpdated",
            Module = "Settings",
            EntityName = nameof(WorkCalendarEntry),
            EntityId = entity.Id.ToString(),
            OldValues = oldValues,
            NewValues = $"BranchId={entity.BranchId};Date={entity.CalendarDate};Type={entity.DayType};Name={entity.Name};Recurring={entity.IsRecurringAnnual};Active={entity.IsActive}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapWorkCalendarEntry(entity);
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

    private static AttendanceRuleProfileResponse MapAttendanceRuleProfile(AttendanceRuleProfile item)
    {
        return new AttendanceRuleProfileResponse(
            item.Id,
            item.Name,
            item.ScopeType,
            item.BranchId,
            item.Branch?.Name,
            item.ShiftId,
            item.Shift?.Name,
            item.Priority,
            item.IsActive,
            item.LateGraceMinutes,
            item.HalfDayThresholdMinutes,
            item.MinPresentMinutes,
            item.OvertimeThresholdMinutes,
            item.EarlyOutGraceMinutes,
            item.ShortLeaveDeductionMinutes,
            item.EnableMissedPunchDetection);
    }

    private static WorkCalendarEntryResponse MapWorkCalendarEntry(WorkCalendarEntry item)
    {
        return new WorkCalendarEntryResponse(
            item.Id,
            item.BranchId,
            item.Branch?.Name ?? "Global Template",
            item.CalendarDate,
            item.DayType,
            item.Name,
            item.IsRecurringAnnual,
            item.IsActive,
            item.Notes);
    }

    private async Task<(OfficeBranch? branch, ShiftDefinition? shift)> ValidateScopeReferencesAsync(
        string scopeType,
        Guid? branchId,
        Guid? shiftId,
        CancellationToken cancellationToken)
    {
        OfficeBranch? branch = null;
        ShiftDefinition? shift = null;

        if (scopeType == "Global")
        {
            return (null, null);
        }

        if (scopeType == "Branch")
        {
            if (branchId is null)
            {
                throw new InvalidOperationException("Branch scope requires branch selection.");
            }

            branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == branchId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Branch not found.");
            return (branch, null);
        }

        if (shiftId is null)
        {
            throw new InvalidOperationException("Shift scope requires shift selection.");
        }

        shift = await dbContext.ShiftDefinitions.FirstOrDefaultAsync(x => x.Id == shiftId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Shift not found.");

        return (null, shift);
    }

    private static string NormalizeScope(string scopeType)
    {
        var value = scopeType?.Trim().ToLowerInvariant();
        return value switch
        {
            "global" => "Global",
            "branch" => "Branch",
            "shift" => "Shift",
            _ => throw new InvalidOperationException("Scope type must be Global, Branch, or Shift.")
        };
    }

    private static int? NormalizeNullableNonNegative(int? value) => value is null ? null : Math.Max(value.Value, 0);

    private async Task<(OfficeBranch? branch, string dayType, string name, string? notes)> ValidateWorkCalendarInputAsync(
        UpsertWorkCalendarEntryRequest request,
        CancellationToken cancellationToken)
    {
        OfficeBranch? branch = null;
        if (request.BranchId is not null)
        {
            branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Id == request.BranchId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Branch not found.");
        }

        var normalizedDayType = NormalizeWorkCalendarDayType(request.DayType);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Entry name is required.");
        }

        var normalizedName = request.Name.Trim();
        var normalizedNotes = NormalizeNullable(request.Notes);
        return (branch, normalizedDayType, normalizedName, normalizedNotes);
    }

    private static string NormalizeWorkCalendarDayType(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "publicholiday" => "PublicHoliday",
            "customnonworkingday" => "CustomNonWorkingDay",
            "specialworkingday" => "SpecialWorkingDay",
            _ => throw new InvalidOperationException("Day type must be PublicHoliday, CustomNonWorkingDay, or SpecialWorkingDay.")
        };
    }

    private static void ValidateYearMonth(int year, int month)
    {
        if (year is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Year must be between 2000 and 2100.");
        }

        if (month is < 1 or > 12)
        {
            throw new InvalidOperationException("Month must be between 1 and 12.");
        }
    }

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TimeOnly ParseTime(string value, TimeOnly fallback)
    {
        return TimeOnly.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
