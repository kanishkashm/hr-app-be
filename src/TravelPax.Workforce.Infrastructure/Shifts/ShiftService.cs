using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Shifts;
using TravelPax.Workforce.Contracts.Shifts;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Shifts;

public sealed class ShiftService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService) : IShiftService
{
    public async Task<IReadOnlyCollection<ShiftResponse>> GetShiftsAsync(Guid? branchId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ShiftDefinitions.Include(x => x.Branch).AsQueryable();
        if (branchId is not null) query = query.Where(x => x.BranchId == branchId.Value);
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return items.Select(MapShift).ToArray();
    }

    public async Task<ShiftResponse> CreateShiftAsync(UpsertShiftRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var entity = new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            BranchId = request.BranchId,
            ShiftType = NormalizeShiftType(request.ShiftType),
            StartTime = ParseTime(request.StartTime, new TimeOnly(9, 0)),
            EndTime = ParseTime(request.EndTime, new TimeOnly(18, 0)),
            FlexMinutes = Math.Max(request.FlexMinutes, 0),
            GraceMinutes = Math.Max(request.GraceMinutes, 0),
            MinHalfDayMinutes = Math.Max(request.MinHalfDayMinutes, 0),
            MinPresentMinutes = Math.Max(request.MinPresentMinutes, 0),
            OvertimeAfterMinutes = Math.Max(request.OvertimeAfterMinutes, 0),
            IsActive = request.IsActive,
            CreatedBy = actorId,
            UpdatedBy = actorId
        };

        dbContext.ShiftDefinitions.Add(entity);
        await WriteAuditAsync(actorId, "ShiftCreated", nameof(ShiftDefinition), entity.Id, $"Code={entity.Code};Name={entity.Name};Type={entity.ShiftType}", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        return MapShift(entity);
    }

    public async Task<ShiftResponse> UpdateShiftAsync(Guid shiftId, UpsertShiftRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var entity = await dbContext.ShiftDefinitions.Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == shiftId, cancellationToken)
            ?? throw new InvalidOperationException("Shift not found.");
        var oldValues = $"Code={entity.Code};Name={entity.Name};Type={entity.ShiftType};Start={entity.StartTime};End={entity.EndTime};Active={entity.IsActive}";

        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.BranchId = request.BranchId;
        entity.ShiftType = NormalizeShiftType(request.ShiftType);
        entity.StartTime = ParseTime(request.StartTime, new TimeOnly(9, 0));
        entity.EndTime = ParseTime(request.EndTime, new TimeOnly(18, 0));
        entity.FlexMinutes = Math.Max(request.FlexMinutes, 0);
        entity.GraceMinutes = Math.Max(request.GraceMinutes, 0);
        entity.MinHalfDayMinutes = Math.Max(request.MinHalfDayMinutes, 0);
        entity.MinPresentMinutes = Math.Max(request.MinPresentMinutes, 0);
        entity.OvertimeAfterMinutes = Math.Max(request.OvertimeAfterMinutes, 0);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorId;

        await WriteAuditAsync(actorId, "ShiftUpdated", nameof(ShiftDefinition), entity.Id, $"Code={entity.Code};Name={entity.Name};Type={entity.ShiftType};Start={entity.StartTime};End={entity.EndTime};Active={entity.IsActive}", cancellationToken, oldValues);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        return MapShift(entity);
    }

    public async Task<IReadOnlyCollection<ShiftAssignmentResponse>> GetAssignmentsAsync(Guid? userId, Guid? shiftId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmployeeShiftAssignments
            .Include(x => x.User)
            .Include(x => x.Shift)
            .AsQueryable();
        if (userId is not null) query = query.Where(x => x.UserId == userId.Value);
        if (shiftId is not null) query = query.Where(x => x.ShiftId == shiftId.Value);
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.User.DisplayName)
            .ToListAsync(cancellationToken);
        return items.Select(MapAssignment).ToArray();
    }

    public async Task<ShiftAssignmentResponse> CreateAssignmentAsync(UpsertShiftAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        await EnsureUserAndShiftAsync(request.UserId, request.ShiftId, cancellationToken);

        var entity = new EmployeeShiftAssignment
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ShiftId = request.ShiftId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive,
            CreatedBy = actorId,
            UpdatedBy = actorId
        };
        dbContext.EmployeeShiftAssignments.Add(entity);
        await WriteAuditAsync(actorId, "ShiftAssignmentCreated", nameof(EmployeeShiftAssignment), entity.Id, $"User={entity.UserId};Shift={entity.ShiftId};From={entity.EffectiveFrom};To={entity.EffectiveTo}", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.User).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Shift).LoadAsync(cancellationToken);
        return MapAssignment(entity);
    }

    public async Task<ShiftAssignmentResponse> UpdateAssignmentAsync(Guid assignmentId, UpsertShiftAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var entity = await dbContext.EmployeeShiftAssignments
            .Include(x => x.User)
            .Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Shift assignment not found.");

        await EnsureUserAndShiftAsync(request.UserId, request.ShiftId, cancellationToken);
        var oldValues = $"User={entity.UserId};Shift={entity.ShiftId};From={entity.EffectiveFrom};To={entity.EffectiveTo};Active={entity.IsActive}";
        entity.UserId = request.UserId;
        entity.ShiftId = request.ShiftId;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorId;

        await WriteAuditAsync(actorId, "ShiftAssignmentUpdated", nameof(EmployeeShiftAssignment), entity.Id, $"User={entity.UserId};Shift={entity.ShiftId};From={entity.EffectiveFrom};To={entity.EffectiveTo};Active={entity.IsActive}", cancellationToken, oldValues);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.User).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Shift).LoadAsync(cancellationToken);
        return MapAssignment(entity);
    }

    public async Task<IReadOnlyCollection<ShiftAssignmentRuleResponse>> GetAssignmentRulesAsync(Guid? branchId, string? department, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ShiftAssignmentRules
            .Include(x => x.Branch)
            .Include(x => x.Shift)
            .AsQueryable();
        if (branchId is not null) query = query.Where(x => x.BranchId == branchId.Value);
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.Department == department);
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Priority).ThenBy(x => x.Department).ToListAsync(cancellationToken);
        return items.Select(MapRule).ToArray();
    }

    public async Task<ShiftAssignmentRuleResponse> CreateAssignmentRuleAsync(UpsertShiftAssignmentRuleRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        await EnsureShiftAsync(request.ShiftId, cancellationToken);

        var entity = new ShiftAssignmentRule
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            Department = NormalizeNullable(request.Department),
            Team = NormalizeNullable(request.Team),
            ShiftId = request.ShiftId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Priority = Math.Clamp(request.Priority, 1, 9999),
            IsActive = request.IsActive,
            CreatedBy = actorId,
            UpdatedBy = actorId
        };
        dbContext.ShiftAssignmentRules.Add(entity);
        await WriteAuditAsync(actorId, "ShiftAssignmentRuleCreated", nameof(ShiftAssignmentRule), entity.Id, $"Shift={entity.ShiftId};Branch={entity.BranchId};Department={entity.Department};Team={entity.Team};Priority={entity.Priority}", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Shift).LoadAsync(cancellationToken);
        return MapRule(entity);
    }

    public async Task<ShiftAssignmentRuleResponse> UpdateAssignmentRuleAsync(Guid ruleId, UpsertShiftAssignmentRuleRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var entity = await dbContext.ShiftAssignmentRules
            .Include(x => x.Branch)
            .Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.Id == ruleId, cancellationToken)
            ?? throw new InvalidOperationException("Shift assignment rule not found.");

        await EnsureShiftAsync(request.ShiftId, cancellationToken);
        var oldValues = $"Shift={entity.ShiftId};Branch={entity.BranchId};Department={entity.Department};Team={entity.Team};Priority={entity.Priority};Active={entity.IsActive}";
        entity.BranchId = request.BranchId;
        entity.Department = NormalizeNullable(request.Department);
        entity.Team = NormalizeNullable(request.Team);
        entity.ShiftId = request.ShiftId;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Priority = Math.Clamp(request.Priority, 1, 9999);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorId;

        await WriteAuditAsync(actorId, "ShiftAssignmentRuleUpdated", nameof(ShiftAssignmentRule), entity.Id, $"Shift={entity.ShiftId};Branch={entity.BranchId};Department={entity.Department};Team={entity.Team};Priority={entity.Priority};Active={entity.IsActive}", cancellationToken, oldValues);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Shift).LoadAsync(cancellationToken);
        return MapRule(entity);
    }

    public async Task<IReadOnlyCollection<ShiftOverrideResponse>> GetOverridesAsync(Guid? userId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ShiftOverrides
            .Include(x => x.User)
            .Include(x => x.Shift)
            .AsQueryable();
        if (userId is not null) query = query.Where(x => x.UserId == userId.Value);
        if (fromDate is not null) query = query.Where(x => x.Date >= fromDate.Value);
        if (toDate is not null) query = query.Where(x => x.Date <= toDate.Value);

        var items = await query.OrderByDescending(x => x.Date).ThenBy(x => x.User.DisplayName).ToListAsync(cancellationToken);
        return items.Select(MapOverride).ToArray();
    }

    public async Task<ShiftOverrideResponse> CreateOverrideAsync(UpsertShiftOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        await EnsureUserAndShiftAsync(request.UserId, request.ShiftId, cancellationToken);
        var entity = new ShiftOverride
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Date = request.Date,
            ShiftId = request.ShiftId,
            Reason = request.Reason.Trim(),
            Status = NormalizeStatus(request.Status),
            CreatedBy = actorId,
            UpdatedBy = actorId
        };
        dbContext.ShiftOverrides.Add(entity);
        await WriteAuditAsync(actorId, "ShiftOverrideCreated", nameof(ShiftOverride), entity.Id, $"User={entity.UserId};Date={entity.Date};Shift={entity.ShiftId};Status={entity.Status}", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.User).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Shift).LoadAsync(cancellationToken);
        return MapOverride(entity);
    }

    public async Task<ShiftOverrideResponse> UpdateOverrideAsync(Guid overrideId, UpsertShiftOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUserService.UserId;
        var entity = await dbContext.ShiftOverrides
            .Include(x => x.User)
            .Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.Id == overrideId, cancellationToken)
            ?? throw new InvalidOperationException("Shift override not found.");
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        await EnsureUserAndShiftAsync(request.UserId, request.ShiftId, cancellationToken);
        var oldValues = $"User={entity.UserId};Date={entity.Date};Shift={entity.ShiftId};Status={entity.Status}";
        entity.UserId = request.UserId;
        entity.Date = request.Date;
        entity.ShiftId = request.ShiftId;
        entity.Reason = request.Reason.Trim();
        entity.Status = NormalizeStatus(request.Status);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorId;

        await WriteAuditAsync(actorId, "ShiftOverrideUpdated", nameof(ShiftOverride), entity.Id, $"User={entity.UserId};Date={entity.Date};Shift={entity.ShiftId};Status={entity.Status}", cancellationToken, oldValues);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.User).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Shift).LoadAsync(cancellationToken);
        return MapOverride(entity);
    }

    public async Task<IReadOnlyCollection<ShiftCalendarItemResponse>> GetCalendarAsync(DateOnly fromDate, DateOnly toDate, Guid? branchId, Guid? userId, CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            throw new InvalidOperationException("fromDate must be before toDate.");
        }

        var usersQuery = dbContext.Users.Where(x => x.Status == "Active").AsQueryable();
        if (branchId is not null) usersQuery = usersQuery.Where(x => x.BranchId == branchId);
        if (userId is not null) usersQuery = usersQuery.Where(x => x.Id == userId);

        var users = await usersQuery.Select(x => new { x.Id, x.EmployeeId, x.DisplayName, x.BranchId, x.Department }).ToListAsync(cancellationToken);
        var userIds = users.Select(x => x.Id).ToArray();

        var shifts = await dbContext.ShiftDefinitions.ToListAsync(cancellationToken);
        var assignments = await dbContext.EmployeeShiftAssignments
            .Where(x => x.IsActive && userIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        var rules = await dbContext.ShiftAssignmentRules
            .Where(x => x.IsActive)
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);
        var overrides = await dbContext.ShiftOverrides
            .Where(x => userIds.Contains(x.UserId) && x.Date >= fromDate && x.Date <= toDate && x.Status == "Approved")
            .ToListAsync(cancellationToken);

        var results = new List<ShiftCalendarItemResponse>();
        foreach (var user in users)
        {
            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                var resolvedShift = ResolveShift(user.Id, user.BranchId, user.Department, date, shifts, assignments, rules, overrides);
                if (resolvedShift is null)
                {
                    continue;
                }

                results.Add(new ShiftCalendarItemResponse(
                    date,
                    user.Id,
                    user.DisplayName,
                    user.EmployeeId,
                    resolvedShift.Id,
                    resolvedShift.Name,
                    resolvedShift.ShiftType,
                    resolvedShift.StartTime.ToString("HH:mm"),
                    resolvedShift.EndTime.ToString("HH:mm"),
                    resolvedShift.GraceMinutes,
                    resolvedShift.FlexMinutes));
            }
        }

        return results.OrderBy(x => x.Date).ThenBy(x => x.EmployeeName).ToArray();
    }

    private static ShiftDefinition? ResolveShift(
        Guid userId,
        Guid? branchId,
        string? department,
        DateOnly date,
        IReadOnlyCollection<ShiftDefinition> shifts,
        IReadOnlyCollection<EmployeeShiftAssignment> assignments,
        IReadOnlyCollection<ShiftAssignmentRule> rules,
        IReadOnlyCollection<ShiftOverride> overrides)
    {
        var overrideMatch = overrides.FirstOrDefault(x => x.UserId == userId && x.Date == date);
        if (overrideMatch is not null)
        {
            return shifts.FirstOrDefault(x => x.Id == overrideMatch.ShiftId && x.IsActive);
        }

        var assignmentMatch = assignments
            .Where(x => x.UserId == userId && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefault();
        if (assignmentMatch is not null)
        {
            return shifts.FirstOrDefault(x => x.Id == assignmentMatch.ShiftId && x.IsActive);
        }

        var ruleMatch = rules
            .Where(x =>
                (x.BranchId == null || x.BranchId == branchId)
                && (string.IsNullOrWhiteSpace(x.Department) || x.Department == department)
                && x.EffectiveFrom <= date
                && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderBy(x => x.Priority)
            .FirstOrDefault();
        return ruleMatch is null ? null : shifts.FirstOrDefault(x => x.Id == ruleMatch.ShiftId && x.IsActive);
    }

    private async Task EnsureUserAndShiftAsync(Guid userId, Guid shiftId, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User not found.");
        }

        await EnsureShiftAsync(shiftId, cancellationToken);
    }

    private async Task EnsureShiftAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        var shiftExists = await dbContext.ShiftDefinitions.AnyAsync(x => x.Id == shiftId, cancellationToken);
        if (!shiftExists)
        {
            throw new InvalidOperationException("Shift not found.");
        }
    }

    private async Task WriteAuditAsync(
        Guid? actorId,
        string action,
        string entityName,
        Guid entityId,
        string newValues,
        CancellationToken cancellationToken,
        string? oldValues = null)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = action,
            Module = "Shifts",
            EntityName = entityName,
            EntityId = entityId.ToString(),
            OldValues = oldValues,
            NewValues = newValues
        });

        await Task.CompletedTask;
    }

    private static string NormalizeShiftType(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Fixed" : value.Trim();
        return normalized switch
        {
            "Flexible" => "Flexible",
            "Overnight" => "Overnight",
            _ => "Fixed"
        };
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Approved" : value.Trim();
        return normalized switch
        {
            "Pending" => "Pending",
            "Rejected" => "Rejected",
            _ => "Approved"
        };
    }

    private static TimeOnly ParseTime(string value, TimeOnly fallback)
    {
        return TimeOnly.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ShiftResponse MapShift(ShiftDefinition x) => new(
        x.Id,
        x.Code,
        x.Name,
        x.BranchId,
        x.Branch?.Name ?? "All Branches",
        x.ShiftType,
        x.StartTime.ToString("HH:mm"),
        x.EndTime.ToString("HH:mm"),
        x.FlexMinutes,
        x.GraceMinutes,
        x.MinHalfDayMinutes,
        x.MinPresentMinutes,
        x.OvertimeAfterMinutes,
        x.IsActive);

    private static ShiftAssignmentResponse MapAssignment(EmployeeShiftAssignment x) => new(
        x.Id,
        x.UserId,
        x.User.DisplayName,
        x.User.EmployeeId,
        x.ShiftId,
        x.Shift.Name,
        x.EffectiveFrom,
        x.EffectiveTo,
        x.IsActive);

    private static ShiftAssignmentRuleResponse MapRule(ShiftAssignmentRule x) => new(
        x.Id,
        x.BranchId,
        x.Branch?.Name ?? "All Branches",
        x.Department,
        x.Team,
        x.ShiftId,
        x.Shift.Name,
        x.EffectiveFrom,
        x.EffectiveTo,
        x.Priority,
        x.IsActive);

    private static ShiftOverrideResponse MapOverride(ShiftOverride x) => new(
        x.Id,
        x.UserId,
        x.User.DisplayName,
        x.User.EmployeeId,
        x.Date,
        x.ShiftId,
        x.Shift.Name,
        x.Reason,
        x.Status);
}

