using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Leave;
using TravelPax.Workforce.Application.Abstractions.Notifications;
using TravelPax.Workforce.Contracts.Leave;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Leave;

public sealed class LeaveService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor,
    INotificationService notificationService,
    IEmailOutboxService emailOutboxService) : ILeaveService
{
    public async Task<LeaveRequestResponse> CreateMyRequestAsync(LeaveRequestCreateRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        await EnsureRangeNotFinalizedAsync(request.StartDate, request.EndDate, actor.BranchId, cancellationToken);
        if (request.StartDate > request.EndDate)
        {
            throw new InvalidOperationException("Start date must be before or equal to end date.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        var leaveType = string.IsNullOrWhiteSpace(request.LeaveType) ? "Annual" : request.LeaveType.Trim();
        var dayPortion = NormalizeDayPortion(request.DayPortion);
        if (dayPortion != "FullDay" && request.StartDate != request.EndDate)
        {
            throw new InvalidOperationException("Half-day leave is only supported for single-day requests.");
        }

        var totalDays = await CalculateRequestedLeaveDaysAsync(request.StartDate, request.EndDate, dayPortion, cancellationToken);
        if (totalDays <= 0)
        {
            throw new InvalidOperationException("Selected range has no working leave days after weekend/holiday rules.");
        }

        if (totalDays > 31m)
        {
            throw new InvalidOperationException("Leave request cannot exceed 31 days in one submission.");
        }

        var hasOverlap = await dbContext.LeaveRequests.AnyAsync(
            x => x.UserId == actor.Id
                 && (x.Status == "Pending" || x.Status == "Approved")
                 && x.StartDate <= request.EndDate
                 && x.EndDate >= request.StartDate,
            cancellationToken);
        if (hasOverlap)
        {
            throw new InvalidOperationException("You already have an overlapping pending/approved leave request.");
        }

        var targetYear = request.StartDate.Year;
        var balance = await EnsureLeaveBalanceAsync(actor, leaveType, targetYear, cancellationToken);
        var pendingDays = await GetPendingDaysAsync(actor.Id, leaveType, targetYear, excludeRequestId: null, cancellationToken);
        var remainingDays = balance.AllocatedDays + balance.CarryForwardDays + balance.ManualAdjustmentDays - balance.UsedDays - pendingDays;
        if (totalDays > remainingDays)
        {
            throw new InvalidOperationException($"Insufficient leave balance. Remaining days: {remainingDays:0.0}.");
        }

        var entity = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = actor.Id,
            LeaveType = leaveType,
            DayPortion = dayPortion,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedBy = actor.Id,
            UpdatedBy = actor.Id
        };

        dbContext.LeaveRequests.Add(entity);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = "LeaveRequested",
            Module = "Leave",
            EntityName = nameof(LeaveRequest),
            EntityId = entity.Id.ToString(),
            NewValues = $"Type={entity.LeaveType};DayPortion={entity.DayPortion};From={entity.StartDate:yyyy-MM-dd};To={entity.EndDate:yyyy-MM-dd};Days={entity.TotalDays:0.0};Status={entity.Status}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.User).LoadAsync(cancellationToken);
        await NotifyLeaveSubmittedAsync(entity, actor, cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyCollection<LeaveRequestResponse>> GetMyRequestsAsync(int take, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var items = await dbContext.LeaveRequests
            .Include(x => x.User)
            .Include(x => x.ReviewedByUser)
            .Where(x => x.UserId == actor.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 120))
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToArray();
    }

    public async Task<LeaveRequestListResponse> GetRequestsAsync(
        string? status,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.LeaveRequests
            .Include(x => x.User)
            .Include(x => x.ReviewedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (userId is not null)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new LeaveRequestListResponse(items.Select(Map).ToArray(), total);
    }

    public async Task<LeaveRequestResponse> ReviewRequestAsync(Guid requestId, LeaveRequestReviewRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var entity = await dbContext.LeaveRequests
            .Include(x => x.User)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Leave request not found.");
        await EnsureRangeNotFinalizedAsync(entity.StartDate, entity.EndDate, entity.User.BranchId, cancellationToken);

        if (entity.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending leave requests can be reviewed.");
        }

        if (request.Approve)
        {
            var balance = await EnsureLeaveBalanceAsync(entity.User, entity.LeaveType, entity.StartDate.Year, cancellationToken);
            var pendingExcludingThis = await GetPendingDaysAsync(entity.UserId, entity.LeaveType, entity.StartDate.Year, entity.Id, cancellationToken);
            var remainingBeforeApproval = balance.AllocatedDays + balance.CarryForwardDays + balance.ManualAdjustmentDays - balance.UsedDays - pendingExcludingThis;
            if (entity.TotalDays > remainingBeforeApproval)
            {
                throw new InvalidOperationException($"Cannot approve. Remaining balance is {remainingBeforeApproval:0.0} day(s).");
            }

            balance.UsedDays += entity.TotalDays;
            balance.UpdatedAt = DateTimeOffset.UtcNow;
            balance.UpdatedBy = actor.Id;
        }

        entity.Status = request.Approve ? "Approved" : "Rejected";
        entity.ReviewedByUserId = actor.Id;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewerNote = string.IsNullOrWhiteSpace(request.ReviewerNote) ? null : request.ReviewerNote.Trim();
        entity.UpdatedBy = actor.Id;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = request.Approve ? "LeaveApproved" : "LeaveRejected",
            Module = "Leave",
            EntityName = nameof(LeaveRequest),
            EntityId = entity.Id.ToString(),
            NewValues = $"Status={entity.Status};ReviewerNote={entity.ReviewerNote}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.ReviewedByUser).LoadAsync(cancellationToken);
        await notificationService.PublishToUsersAsync(
            [entity.UserId],
            "LeaveReview",
            $"Leave request {entity.Status}",
            $"Your {entity.LeaveType} leave request ({entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd}) was {entity.Status.ToLowerInvariant()}.",
            nameof(LeaveRequest),
            entity.Id.ToString(),
            actor.Id,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(entity.User.Email))
        {
            await emailOutboxService.QueueAsync(
                [entity.User.Email],
                $"TravelPax Leave Request {entity.Status}",
                $"Hello {entity.User.DisplayName}, your {entity.LeaveType} leave request from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} was {entity.Status.ToLowerInvariant()}.",
                null,
                actor.Id,
                cancellationToken);
        }
        return Map(entity);
    }

    private async Task NotifyLeaveSubmittedAsync(LeaveRequest leaveRequest, AppUser actor, CancellationToken cancellationToken)
    {
        var roleIds = await dbContext.Roles
            .Where(x => x.Name == RoleCodes.SuperAdmin || x.Name == RoleCodes.HrAdmin || x.Name == RoleCodes.OperationsManager)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var reviewerIds = await dbContext.UserRoles
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        await notificationService.PublishToUsersAsync(
            reviewerIds,
            "LeaveRequest",
            "New leave request submitted",
            $"{actor.DisplayName} requested {leaveRequest.LeaveType} leave ({leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}, {leaveRequest.TotalDays:0.0} day/s).",
            nameof(LeaveRequest),
            leaveRequest.Id.ToString(),
            actor.Id,
            cancellationToken);

        var reviewerEmails = await dbContext.Users
            .Where(x => reviewerIds.Contains(x.Id) && x.Email != null)
            .Select(x => x.Email!)
            .ToListAsync(cancellationToken);

        await emailOutboxService.QueueAsync(
            reviewerEmails,
            "TravelPax New Leave Request",
            $"{actor.DisplayName} requested {leaveRequest.LeaveType} leave from {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}. Please review in TravelPax Workforce.",
            null,
            actor.Id,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveBalanceResponse>> GetMyBalancesAsync(int? year, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        return await BuildBalancesQuery(year, actor.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveBalanceResponse>> GetBalancesAsync(int? year, Guid? userId, CancellationToken cancellationToken = default)
    {
        return await BuildBalancesQuery(year, userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeavePolicyResponse>> GetPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var policies = await dbContext.LeavePolicies
            .Include(x => x.Branch)
            .OrderBy(x => x.LeaveType)
            .ThenBy(x => x.EmploymentType)
            .ToListAsync(cancellationToken);

        return policies.Select(MapPolicy).ToArray();
    }

    public async Task<LeavePolicyResponse> UpsertPolicyAsync(Guid? policyId, LeavePolicyUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);

        if (request.AnnualAllocationDays < 0 || request.MaxCarryForwardDays < 0)
        {
            throw new InvalidOperationException("Allocation values must be zero or positive.");
        }

        var leaveType = request.LeaveType.Trim();
        var employmentType = request.EmploymentType.Trim();

        LeavePolicy entity;
        if (policyId is not null)
        {
            entity = await dbContext.LeavePolicies
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == policyId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Leave policy not found.");
        }
        else
        {
            entity = await dbContext.LeavePolicies
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(
                    x => x.LeaveType == leaveType
                         && x.EmploymentType == employmentType
                         && x.BranchId == request.BranchId,
                    cancellationToken)
                ?? new LeavePolicy
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = actor.Id
                };

            if (entity.CreatedAt == default)
            {
                dbContext.LeavePolicies.Add(entity);
            }
        }

        entity.LeaveType = leaveType;
        entity.EmploymentType = employmentType;
        entity.BranchId = request.BranchId;
        entity.AnnualAllocationDays = request.AnnualAllocationDays;
        entity.MaxCarryForwardDays = request.MaxCarryForwardDays;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actor.Id;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = "LeavePolicyUpserted",
            Module = "Leave",
            EntityName = nameof(LeavePolicy),
            EntityId = entity.Id.ToString(),
            NewValues = $"Type={entity.LeaveType};Employment={entity.EmploymentType};Allocation={entity.AnnualAllocationDays};Carry={entity.MaxCarryForwardDays};Active={entity.IsActive}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(x => x.Branch).LoadAsync(cancellationToken);
        return MapPolicy(entity);
    }

    private async Task<IReadOnlyCollection<LeaveBalanceResponse>> BuildBalancesQuery(int? year, Guid? userId, CancellationToken cancellationToken)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var query = dbContext.LeaveBalances
            .Include(x => x.User)
            .Where(x => x.Year == targetYear)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var balances = await query
            .OrderBy(x => x.User.DisplayName)
            .ThenBy(x => x.LeaveType)
            .ToListAsync(cancellationToken);

        var pendingLookup = await dbContext.LeaveRequests
            .Where(x => x.Status == "Pending" && x.StartDate.Year == targetYear && (userId == null || x.UserId == userId.Value))
            .GroupBy(x => new { x.UserId, x.LeaveType })
            .Select(x => new { x.Key.UserId, x.Key.LeaveType, PendingDays = x.Sum(y => y.TotalDays) })
            .ToListAsync(cancellationToken);

        return balances.Select(balance =>
        {
            var pendingDays = pendingLookup
                .Where(x => x.UserId == balance.UserId && x.LeaveType == balance.LeaveType)
                .Select(x => x.PendingDays)
                .FirstOrDefault();

            return MapBalance(balance, pendingDays);
        }).ToArray();
    }

    private async Task<LeaveBalance> EnsureLeaveBalanceAsync(AppUser user, string leaveType, int year, CancellationToken cancellationToken)
    {
        var balance = await dbContext.LeaveBalances
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Year == year && x.LeaveType == leaveType, cancellationToken);

        if (balance is not null)
        {
            return balance;
        }

        var policy = await ResolvePolicyAsync(leaveType, user, cancellationToken);
        balance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Year = year,
            LeaveType = leaveType,
            AllocatedDays = policy?.AnnualAllocationDays ?? DefaultAllocation(leaveType),
            CarryForwardDays = 0,
            ManualAdjustmentDays = 0,
            UsedDays = 0,
            CreatedBy = user.Id,
            UpdatedBy = user.Id
        };

        dbContext.LeaveBalances.Add(balance);
        return balance;
    }

    private async Task<LeavePolicy?> ResolvePolicyAsync(string leaveType, AppUser user, CancellationToken cancellationToken)
    {
        var employmentType = string.IsNullOrWhiteSpace(user.EmploymentType) ? "FullTime" : user.EmploymentType;

        var branchSpecific = await dbContext.LeavePolicies
            .Where(x => x.IsActive
                        && x.LeaveType == leaveType
                        && x.EmploymentType == employmentType
                        && x.BranchId == user.BranchId)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (branchSpecific is not null)
        {
            return branchSpecific;
        }

        return await dbContext.LeavePolicies
            .Where(x => x.IsActive
                        && x.LeaveType == leaveType
                        && x.EmploymentType == employmentType
                        && x.BranchId == null)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<decimal> GetPendingDaysAsync(Guid userId, string leaveType, int year, Guid? excludeRequestId, CancellationToken cancellationToken)
    {
        return await dbContext.LeaveRequests
            .Where(x => x.UserId == userId
                        && x.LeaveType == leaveType
                        && x.StartDate.Year == year
                        && x.Status == "Pending"
                        && (excludeRequestId == null || x.Id != excludeRequestId.Value))
            .SumAsync(x => (decimal?)x.TotalDays, cancellationToken) ?? 0m;
    }

    private async Task<decimal> CalculateRequestedLeaveDaysAsync(DateOnly startDate, DateOnly endDate, string dayPortion, CancellationToken cancellationToken)
    {
        var settings = await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
        var weekendDays = ParseWeekendDays(settings.WeekendConfig);
        var holidayDates = ParseHolidayDates(settings.WeekendConfig);

        decimal total = 0m;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (weekendDays.Contains(date.DayOfWeek))
            {
                continue;
            }

            if (holidayDates.Contains(date))
            {
                continue;
            }

            total += 1m;
        }

        if (dayPortion != "FullDay" && startDate == endDate && total >= 1m)
        {
            return 0.5m;
        }

        return total;
    }

    private static HashSet<DayOfWeek> ParseWeekendDays(string? weekendConfig)
    {
        var result = new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
        if (string.IsNullOrWhiteSpace(weekendConfig))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(weekendConfig);
            if (!doc.RootElement.TryGetProperty("days", out var daysElement) || daysElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            result.Clear();
            foreach (var dayElement in daysElement.EnumerateArray())
            {
                if (dayElement.ValueKind == JsonValueKind.String &&
                    Enum.TryParse<DayOfWeek>(dayElement.GetString(), true, out var parsed))
                {
                    result.Add(parsed);
                }
            }

            if (result.Count == 0)
            {
                result.Add(DayOfWeek.Saturday);
                result.Add(DayOfWeek.Sunday);
            }
        }
        catch
        {
            return new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
        }

        return result;
    }

    private static HashSet<DateOnly> ParseHolidayDates(string? weekendConfig)
    {
        var result = new HashSet<DateOnly>();
        if (string.IsNullOrWhiteSpace(weekendConfig))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(weekendConfig);
            if (!doc.RootElement.TryGetProperty("dates", out var datesElement) || datesElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var dateElement in datesElement.EnumerateArray())
            {
                if (dateElement.ValueKind == JsonValueKind.String &&
                    DateOnly.TryParse(dateElement.GetString(), out var parsedDate))
                {
                    result.Add(parsedDate);
                }
            }
        }
        catch
        {
            return new HashSet<DateOnly>();
        }

        return result;
    }

    private static decimal DefaultAllocation(string leaveType)
    {
        return leaveType switch
        {
            "Annual" => 14m,
            "Casual" => 7m,
            "Medical" => 10m,
            _ => 5m
        };
    }

    private static string NormalizeDayPortion(string? dayPortion)
    {
        if (string.IsNullOrWhiteSpace(dayPortion))
        {
            return "FullDay";
        }

        return dayPortion.Trim() switch
        {
            "FirstHalf" => "FirstHalf",
            "SecondHalf" => "SecondHalf",
            _ => "FullDay"
        };
    }

    private async Task EnsureRangeNotFinalizedAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        var rangeStart = new DateOnly(startDate.Year, startDate.Month, 1);
        var rangeEnd = new DateOnly(endDate.Year, endDate.Month, 1);

        for (var monthCursor = rangeStart; monthCursor <= rangeEnd; monthCursor = monthCursor.AddMonths(1))
        {
            var isFinalized = await dbContext.PayrollPeriodFinalizations.AnyAsync(
                x => x.IsFinalized
                     && x.Year == monthCursor.Year
                     && x.Month == monthCursor.Month
                     && (x.BranchId == null || (branchId != null && x.BranchId == branchId)),
                cancellationToken);
            if (isFinalized)
            {
                throw new InvalidOperationException(
                    $"Leave changes are blocked because payroll is finalized for {monthCursor:yyyy-MM}.");
            }
        }
    }

    private async Task<AppUser> GetCurrentUserEntityAsync(CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return await dbContext.Users
            .Include(x => x.Branch)
            .FirstAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);
    }

    private string? GetIpAddress()
    {
        var forwardedFor = httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent() => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private static LeaveRequestResponse Map(LeaveRequest entity)
    {
        return new LeaveRequestResponse(
            entity.Id,
            entity.UserId,
            entity.User.DisplayName,
            entity.User.EmployeeId,
            entity.User.Department ?? string.Empty,
            entity.LeaveType,
            entity.DayPortion,
            entity.StartDate,
            entity.EndDate,
            entity.TotalDays,
            entity.Reason,
            entity.Status,
            entity.CreatedAt,
            entity.ReviewedByUserId,
            entity.ReviewedByUser?.DisplayName,
            entity.ReviewedAt,
            entity.ReviewerNote);
    }

    private static LeaveBalanceResponse MapBalance(LeaveBalance entity, decimal pendingDays)
    {
        var remaining = entity.AllocatedDays + entity.CarryForwardDays + entity.ManualAdjustmentDays - entity.UsedDays - pendingDays;
        return new LeaveBalanceResponse(
            entity.Id,
            entity.UserId,
            entity.User.DisplayName,
            entity.User.EmployeeId,
            entity.Year,
            entity.LeaveType,
            entity.AllocatedDays,
            entity.CarryForwardDays,
            entity.ManualAdjustmentDays,
            entity.UsedDays,
            pendingDays,
            remaining);
    }

    private static LeavePolicyResponse MapPolicy(LeavePolicy entity)
    {
        return new LeavePolicyResponse(
            entity.Id,
            entity.LeaveType,
            entity.EmploymentType,
            entity.BranchId,
            entity.Branch?.Name,
            entity.AnnualAllocationDays,
            entity.MaxCarryForwardDays,
            entity.IsActive);
    }
}
