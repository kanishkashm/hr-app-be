using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.Attendance;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Contracts.Attendance;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Attendance;

public sealed class AttendanceService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor,
    INetworkValidationService networkValidationService) : IAttendanceService
{
    private const string TimezoneId = "Asia/Colombo";

    public async Task<AttendanceTodayResponse> GetMyTodayAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        var today = GetBusinessDate();
        var record = await LoadAttendanceRecordAsync(user.Id, today, cancellationToken);

        return new AttendanceTodayResponse(
            today,
            TimezoneId,
            GetIpAddress(),
            record?.Status ?? "NotClockedIn",
            record is null,
            record is not null && record.ClockOutAt is null,
            record is null ? null : MapRecord(record));
    }

    public async Task<AttendanceRecordResponse> ClockInAsync(ClockAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        var today = GetBusinessDate();
        await EnsureAttendanceDateUnlockedAsync(today, user.BranchId, cancellationToken);
        var existing = await LoadAttendanceRecordAsync(user.Id, today, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("You have already clocked in for today.");
        }

        var now = DateTimeOffset.UtcNow;
        var settings = await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
        var shift = await ResolveShiftForUserDateAsync(user, today, cancellationToken);
        var effectiveRules = await ResolveEffectiveRulesAsync(settings, user.BranchId, shift?.Id, shift, cancellationToken);
        var networkResult = await networkValidationService.ValidateAsync(user.BranchId, GetIpAddress(), cancellationToken);
        var ruleComputed = AttendanceRulesEngine.Evaluate(today, now, null, settings, shift, effectiveRules, GetBusinessDate());
        var lateMinutes = ruleComputed.LateMinutes;

        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BranchId = user.BranchId,
            AttendanceDate = today,
            ClockInAt = now,
            Status = "PendingClockOut",
            IsLate = lateMinutes > 0,
            LateMinutes = lateMinutes > 0 ? lateMinutes : null,
            ClockInIp = GetIpAddress(),
            ClockInUserAgent = GetUserAgent(),
            ClockInDeviceSummary = GetUserAgent(),
            ClockInNetworkValidation = networkResult.Status,
            ClockInNetworkRuleId = networkResult.MatchedRuleId,
            Notes = request.Notes,
            CreatedBy = user.Id,
            UpdatedBy = user.Id
        };

        dbContext.AttendanceRecords.Add(record);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "ClockIn",
            Module = "Attendance",
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id.ToString(),
            NewValues = $"ClockInAt={record.ClockInAt:o};Status={record.Status}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(record).Reference(x => x.User).LoadAsync(cancellationToken);
        return MapRecord(record, user);
    }

    public async Task<AttendanceRecordResponse> ClockOutAsync(ClockAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        var today = GetBusinessDate();
        await EnsureAttendanceDateUnlockedAsync(today, user.BranchId, cancellationToken);
        var record = await LoadAttendanceRecordAsync(user.Id, today, cancellationToken)
            ?? throw new InvalidOperationException("You must clock in before clocking out.");

        if (record.ClockOutAt is not null)
        {
            throw new InvalidOperationException("You have already clocked out for today.");
        }

        var now = DateTimeOffset.UtcNow;
        if (record.ClockInAt is null || now <= record.ClockInAt.Value)
        {
            throw new InvalidOperationException("Clock-out must happen after clock-in.");
        }

        var networkResult = await networkValidationService.ValidateAsync(user.BranchId, GetIpAddress(), cancellationToken);

        record.ClockOutAt = now;
        record.ClockOutIp = GetIpAddress();
        record.ClockOutUserAgent = GetUserAgent();
        record.ClockOutDeviceSummary = GetUserAgent();
        record.ClockOutNetworkValidation = networkResult.Status;
        record.ClockOutNetworkRuleId = networkResult.MatchedRuleId;
        var settings = await GetCompanySettingsAsync(cancellationToken);
        var shift = await ResolveShiftForUserDateAsync(user, record.AttendanceDate, cancellationToken);
        var effectiveRules = await ResolveEffectiveRulesAsync(settings, user.BranchId, shift?.Id, shift, cancellationToken);
        ApplyDerivedAttendanceState(record, null, settings, shift, effectiveRules);
        record.Notes = string.IsNullOrWhiteSpace(request.Notes) ? record.Notes : request.Notes;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = user.Id;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "ClockOut",
            Module = "Attendance",
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id.ToString(),
            NewValues = $"ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes}"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapRecord(record, user);
    }

    public async Task<IReadOnlyCollection<AttendanceRecordResponse>> GetMyHistoryAsync(int take, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        var records = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.AttendanceDate)
            .Take(Math.Clamp(take, 1, 90))
            .ToListAsync(cancellationToken);

        return records.Select(MapRecord).ToArray();
    }

    public async Task<AttendanceDetailResponse> GetAttendanceDetailAsync(Guid attendanceId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == attendanceId, cancellationToken)
            ?? throw new InvalidOperationException("Attendance record not found.");

        return MapDetail(record);
    }

    public async Task<AttendanceRecordResponse> CorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var record = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == attendanceId, cancellationToken)
            ?? throw new InvalidOperationException("Attendance record not found.");
        await EnsureAttendanceDateUnlockedAsync(record.AttendanceDate, record.BranchId, cancellationToken);

        if (request.ClockInAt is not null && request.ClockOutAt is not null && request.ClockOutAt <= request.ClockInAt)
        {
            throw new InvalidOperationException("Clock-out must be after clock-in.");
        }

        var oldValues =
            $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes}";

        record.ClockInAt = request.ClockInAt?.ToUniversalTime();
        record.ClockOutAt = request.ClockOutAt?.ToUniversalTime();
        record.Notes = request.Notes;
        var settings = await GetCompanySettingsAsync(cancellationToken);
        var shift = await ResolveShiftForUserDateAsync(record.User, record.AttendanceDate, cancellationToken);
        var effectiveRules = await ResolveEffectiveRulesAsync(record.BranchId, shift?.Id, settings, shift, cancellationToken);
        ApplyDerivedAttendanceState(record, request.Status, settings, shift, effectiveRules);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = actor.Id;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = "AttendanceCorrected",
            Module = "Attendance",
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id.ToString(),
            OldValues = oldValues,
            NewValues =
                $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes};Reason={request.Reason}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapRecord(record);
    }

    public async Task<AttendanceRecordResponse> SelfCorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var record = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == attendanceId && x.UserId == actor.Id, cancellationToken)
            ?? throw new InvalidOperationException("Attendance record not found.");
        await EnsureAttendanceDateUnlockedAsync(record.AttendanceDate, record.BranchId, cancellationToken);

        if (request.ClockInAt is null)
        {
            throw new InvalidOperationException("Clock-in time is required.");
        }

        if (request.ClockOutAt is not null && request.ClockOutAt <= request.ClockInAt)
        {
            throw new InvalidOperationException("Clock-out must be after clock-in.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Please provide a reason for the change.");
        }

        var oldValues =
            $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes}";

        record.ClockInAt = request.ClockInAt.Value.ToUniversalTime();
        record.ClockOutAt = request.ClockOutAt?.ToUniversalTime();
        record.Notes = request.Notes;
        var settings = await GetCompanySettingsAsync(cancellationToken);
        var shift = await ResolveShiftForUserDateAsync(record.User, record.AttendanceDate, cancellationToken);
        var effectiveRules = await ResolveEffectiveRulesAsync(record.BranchId, shift?.Id, settings, shift, cancellationToken);
        ApplyDerivedAttendanceState(record, null, settings, shift, effectiveRules);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = actor.Id;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = "AttendanceSelfCorrected",
            Module = "Attendance",
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id.ToString(),
            OldValues = oldValues,
            NewValues =
                $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes};Reason={request.Reason}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapRecord(record);
    }

    public async Task<AttendanceCorrectionRequestResponse> SubmitCorrectionRequestAsync(
        Guid attendanceId,
        AttendanceCorrectionSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var record = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == attendanceId && x.UserId == actor.Id, cancellationToken)
            ?? throw new InvalidOperationException("Attendance record not found.");
        await EnsureAttendanceDateUnlockedAsync(record.AttendanceDate, record.BranchId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Please provide a reason.");
        }

        if (request.ClockOutAt is not null && request.ClockOutAt <= request.ClockInAt)
        {
            throw new InvalidOperationException("Clock-out must be after clock-in.");
        }

        var existingPending = await dbContext.AttendanceCorrectionRequests
            .AnyAsync(x => x.AttendanceRecordId == attendanceId && x.Status == "Pending", cancellationToken);
        if (existingPending)
        {
            throw new InvalidOperationException("A pending correction request already exists for this attendance record.");
        }

        var item = new AttendanceCorrectionRequest
        {
            Id = Guid.NewGuid(),
            AttendanceRecordId = attendanceId,
            RequestedByUserId = actor.Id,
            RequestType = NormalizeRequestType(request.RequestType),
            RequestedClockInAt = request.ClockInAt.ToUniversalTime(),
            RequestedClockOutAt = request.ClockOutAt?.ToUniversalTime(),
            RequestedNotes = request.Notes,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedBy = actor.Id,
            UpdatedBy = actor.Id
        };

        dbContext.AttendanceCorrectionRequests.Add(item);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = "AttendanceCorrectionRequested",
            Module = "Attendance",
            EntityName = nameof(AttendanceCorrectionRequest),
            EntityId = item.Id.ToString(),
            NewValues =
                $"AttendanceId={attendanceId};ClockInAt={item.RequestedClockInAt:o};ClockOutAt={item.RequestedClockOutAt:o};Reason={item.Reason}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(item).Reference(x => x.RequestedByUser).LoadAsync(cancellationToken);
        await dbContext.Entry(item).Reference(x => x.AttendanceRecord).LoadAsync(cancellationToken);

        return MapCorrection(item);
    }

    public async Task<IReadOnlyCollection<AttendanceCorrectionRequestResponse>> GetMyCorrectionRequestsAsync(
        int take,
        string? requestType,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var query = dbContext.AttendanceCorrectionRequests
            .Include(x => x.AttendanceRecord)
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser)
            .Where(x => x.RequestedByUserId == actor.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(x => x.RequestType == requestType);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 90))
            .ToListAsync(cancellationToken);

        return items.Select(MapCorrection).ToArray();
    }

    public async Task<AttendanceCorrectionRequestListResponse> GetCorrectionRequestsAsync(
        string? status,
        string? requestType,
        bool teamOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.AttendanceCorrectionRequests
            .Include(x => x.AttendanceRecord)
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(x => x.RequestType == requestType);
        }

        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var roleCodes = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == actor.Id
            select role.Code)
            .ToArrayAsync(cancellationToken);

        var managerScopedRole = roleCodes.Contains(RoleCodes.TeamLead) || roleCodes.Contains(RoleCodes.OperationsManager);
        var applyTeamScope = teamOnly || managerScopedRole;

        if (applyTeamScope)
        {
            query = query.Where(x => x.RequestedByUser.ReportingManagerId == actor.Id);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AttendanceCorrectionRequestListResponse(items.Select(MapCorrection).ToArray(), total);
    }

    public async Task<AttendanceExceptionDetailResponse> GetCorrectionRequestDetailAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.AttendanceCorrectionRequests
            .Include(x => x.AttendanceRecord)
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Correction request not found.");

        var timeline = new List<AttendanceExceptionTimelineEventResponse>
        {
            new(
                item.CreatedAt,
                "Requested",
                item.RequestedByUser.DisplayName,
                item.Reason)
        };

        if (item.ReviewedAt is not null)
        {
            timeline.Add(new AttendanceExceptionTimelineEventResponse(
                item.ReviewedAt.Value,
                item.Status == "Approved" ? "Approved" : "Rejected",
                item.ReviewedByUser?.DisplayName ?? "Reviewer",
                item.ReviewerNote));
        }

        if (item.Status == "Approved")
        {
            timeline.Add(new AttendanceExceptionTimelineEventResponse(
                item.UpdatedAt ?? item.ReviewedAt ?? item.CreatedAt,
                "AttendanceLinked",
                "System",
                $"Linked to attendance record {item.AttendanceRecordId}"));
        }

        return new AttendanceExceptionDetailResponse(MapCorrection(item), timeline.OrderBy(x => x.At).ToArray());
    }

    public async Task<AttendanceCorrectionRequestResponse> ReviewCorrectionRequestAsync(
        Guid requestId,
        AttendanceCorrectionReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var item = await dbContext.AttendanceCorrectionRequests
            .Include(x => x.AttendanceRecord)
            .Include(x => x.AttendanceRecord.User)
            .Include(x => x.AttendanceRecord.Branch)
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Correction request not found.");

        if (item.Status != "Pending")
        {
            throw new InvalidOperationException("This correction request is already reviewed.");
        }

        item.Status = request.Approve ? "Approved" : "Rejected";
        item.ReviewedByUserId = actor.Id;
        item.ReviewedAt = DateTimeOffset.UtcNow;
        item.ReviewerNote = request.ReviewerNote;
        item.ReviewIpAddress = GetIpAddress();
        item.UpdatedBy = actor.Id;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Approve)
        {
            var record = item.AttendanceRecord;
            await EnsureAttendanceDateUnlockedAsync(record.AttendanceDate, record.BranchId, cancellationToken);
            var oldValues =
                $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes}";

            record.ClockInAt = item.RequestedClockInAt;
            record.ClockOutAt = item.RequestedClockOutAt;
            record.Notes = item.RequestedNotes;
            var settings = await GetCompanySettingsAsync(cancellationToken);
            var shift = await ResolveShiftForUserDateAsync(record.User, record.AttendanceDate, cancellationToken);
            var effectiveRules = await ResolveEffectiveRulesAsync(record.BranchId, shift?.Id, settings, shift, cancellationToken);
            ApplyDerivedAttendanceState(record, null, settings, shift, effectiveRules);
            record.UpdatedAt = DateTimeOffset.UtcNow;
            record.UpdatedBy = actor.Id;

            dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actor.Id,
                Action = "AttendanceCorrectionApproved",
                Module = "Attendance",
                EntityName = nameof(AttendanceRecord),
                EntityId = record.Id.ToString(),
                OldValues = oldValues,
                NewValues =
                    $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes};RequestId={item.Id}",
                IpAddress = GetIpAddress(),
                UserAgent = GetUserAgent()
            });
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor.Id,
            Action = request.Approve ? "AttendanceCorrectionReviewApproved" : "AttendanceCorrectionReviewRejected",
            Module = "Attendance",
            EntityName = nameof(AttendanceCorrectionRequest),
            EntityId = item.Id.ToString(),
            NewValues = $"Status={item.Status};ReviewerNote={item.ReviewerNote}",
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(item).Reference(x => x.ReviewedByUser).LoadAsync(cancellationToken);

        return MapCorrection(item);
    }

    public async Task<AttendanceListResponse> GetAttendanceAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? userId,
        string? department,
        string? status,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .AsQueryable();

        if (fromDate is not null) query = query.Where(x => x.AttendanceDate >= fromDate.Value);
        if (toDate is not null) query = query.Where(x => x.AttendanceDate <= toDate.Value);
        if (userId is not null) query = query.Where(x => x.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.User.Department == department);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (branchId is not null) query = query.Where(x => x.BranchId == branchId.Value);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.User.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var today = GetBusinessDate();
        var todayRecords = await dbContext.AttendanceRecords
            .Where(x => x.AttendanceDate == today)
            .ToListAsync(cancellationToken);

        var totalEmployees = await dbContext.Users.CountAsync(x => x.Status == "Active", cancellationToken);
        var summary = new AttendanceSummaryResponse(
            totalEmployees,
            todayRecords.Count(x => x.ClockInAt != null),
            Math.Max(totalEmployees - todayRecords.Count(x => x.ClockInAt != null), 0),
            todayRecords.Count(x => x.IsLate),
            todayRecords.Count(x => x.ClockInAt != null && x.ClockOutAt == null));

        return new AttendanceListResponse(items.Select(MapRecord).ToArray(), totalCount, summary);
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

    private Task<AttendanceRecord?> LoadAttendanceRecordAsync(Guid userId, DateOnly attendanceDate, CancellationToken cancellationToken)
    {
        return dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AttendanceDate == attendanceDate, cancellationToken);
    }

    private async Task<CompanySetting> GetCompanySettingsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
    }

    private async Task<AttendanceRulesEngine.EffectiveAttendanceRules> ResolveEffectiveRulesAsync(
        CompanySetting settings,
        Guid? branchId,
        Guid? shiftId,
        ShiftDefinition? shift,
        CancellationToken cancellationToken)
    {
        var profiles = await dbContext.AttendanceRuleProfiles
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        var matchedProfile = AttendanceRulesEngine.ResolveBestProfile(profiles, branchId, shiftId);
        return AttendanceRulesEngine.BuildEffectiveRules(settings, shift, matchedProfile);
    }

    private async Task<AttendanceRulesEngine.EffectiveAttendanceRules> ResolveEffectiveRulesAsync(
        Guid? branchId,
        Guid? shiftId,
        CompanySetting settings,
        ShiftDefinition? shift,
        CancellationToken cancellationToken)
    {
        return await ResolveEffectiveRulesAsync(settings, branchId, shiftId, shift, cancellationToken);
    }

    private static void ApplyDerivedAttendanceState(
        AttendanceRecord record,
        string? requestedStatus,
        CompanySetting settings,
        ShiftDefinition? shift,
        AttendanceRulesEngine.EffectiveAttendanceRules effectiveRules)
    {
        var computed = AttendanceRulesEngine.Evaluate(
            record.AttendanceDate,
            record.ClockInAt,
            record.ClockOutAt,
            settings,
            shift,
            effectiveRules,
            GetBusinessDate());

        record.TotalWorkMinutes = computed.TotalWorkMinutes;
        record.IsLate = computed.IsLate;
        record.LateMinutes = computed.LateMinutes > 0 ? computed.LateMinutes : null;

        if (!string.IsNullOrWhiteSpace(requestedStatus))
        {
            record.Status = requestedStatus;
            return;
        }

        record.Status = computed.SuggestedStatus;
    }

    private static DateOnly GetBusinessDate() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, TimezoneId));

    private async Task<ShiftDefinition?> ResolveShiftForUserDateAsync(AppUser user, DateOnly date, CancellationToken cancellationToken)
    {
        var overrideShiftId = await dbContext.ShiftOverrides
            .Where(x => x.UserId == user.Id && x.Date == date && x.Status == "Approved")
            .Select(x => (Guid?)x.ShiftId)
            .FirstOrDefaultAsync(cancellationToken);
        if (overrideShiftId is not null)
        {
            return await dbContext.ShiftDefinitions.FirstOrDefaultAsync(x => x.Id == overrideShiftId.Value && x.IsActive, cancellationToken);
        }

        var directShiftId = await dbContext.EmployeeShiftAssignments
            .Where(x => x.UserId == user.Id && x.IsActive && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (Guid?)x.ShiftId)
            .FirstOrDefaultAsync(cancellationToken);
        if (directShiftId is not null)
        {
            return await dbContext.ShiftDefinitions.FirstOrDefaultAsync(x => x.Id == directShiftId.Value && x.IsActive, cancellationToken);
        }

        var ruleShiftId = await dbContext.ShiftAssignmentRules
            .Where(x =>
                x.IsActive
                && x.EffectiveFrom <= date
                && (x.EffectiveTo == null || x.EffectiveTo >= date)
                && (x.BranchId == null || x.BranchId == user.BranchId)
                && (x.Department == null || x.Department == user.Department))
            .OrderBy(x => x.Priority)
            .Select(x => (Guid?)x.ShiftId)
            .FirstOrDefaultAsync(cancellationToken);

        return ruleShiftId is null
            ? null
            : await dbContext.ShiftDefinitions.FirstOrDefaultAsync(x => x.Id == ruleShiftId.Value && x.IsActive, cancellationToken);
    }

    private async Task EnsureAttendanceDateUnlockedAsync(DateOnly attendanceDate, Guid? branchId, CancellationToken cancellationToken)
    {
        var isFinalized = await dbContext.PayrollPeriodFinalizations.AnyAsync(
            x => x.IsFinalized
                 && x.Year == attendanceDate.Year
                 && x.Month == attendanceDate.Month
                 && (x.BranchId == null || (branchId != null && x.BranchId == branchId)),
            cancellationToken);
        if (isFinalized)
        {
            throw new InvalidOperationException(
                $"Attendance for period {attendanceDate:yyyy-MM} is finalized for payroll and cannot be changed.");
        }

        var isLocked = await dbContext.AttendancePeriodLocks.AnyAsync(
            x => x.IsLocked
                 && x.Year == attendanceDate.Year
                 && x.Month == attendanceDate.Month
                 && (x.BranchId == null || (branchId != null && x.BranchId == branchId)),
            cancellationToken);

        if (!isLocked)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Attendance for period {attendanceDate:yyyy-MM} is locked for payroll finalization. Please contact HR Admin.");
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

    private static AttendanceRecordResponse MapRecord(AttendanceRecord record) => MapRecord(record, record.User);

    private static AttendanceRecordResponse MapRecord(AttendanceRecord record, AppUser? user)
    {
        return new AttendanceRecordResponse(
            record.Id,
            record.UserId,
            user?.DisplayName ?? string.Empty,
            user?.EmployeeId ?? string.Empty,
            user?.Department ?? string.Empty,
            user?.Designation ?? string.Empty,
            record.Branch?.Name ?? user?.Branch?.Name ?? string.Empty,
            record.AttendanceDate,
            record.ClockInAt,
            record.ClockOutAt,
            record.TotalWorkMinutes,
            record.Status,
            record.IsLate,
            record.LateMinutes,
            record.ClockInIp,
            record.ClockOutIp,
            record.ClockInNetworkValidation,
            record.ClockOutNetworkValidation,
            record.Notes);
    }

    private static AttendanceDetailResponse MapDetail(AttendanceRecord record)
    {
        return new AttendanceDetailResponse(
            record.Id,
            record.UserId,
            record.User.DisplayName,
            record.User.EmployeeId,
            record.User.Department ?? string.Empty,
            record.User.Designation ?? string.Empty,
            record.Branch?.Name ?? record.User.Branch?.Name ?? string.Empty,
            record.AttendanceDate,
            record.ClockInAt,
            record.ClockOutAt,
            record.TotalWorkMinutes,
            record.Status,
            record.IsLate,
            record.LateMinutes,
            record.ClockInIp,
            record.ClockOutIp,
            record.ClockInUserAgent,
            record.ClockOutUserAgent,
            record.ClockInDeviceSummary,
            record.ClockOutDeviceSummary,
            record.ClockInNetworkValidation,
            record.ClockOutNetworkValidation,
            record.Notes,
            record.CreatedAt,
            record.UpdatedAt,
            record.CreatedBy,
            record.UpdatedBy);
    }

    private static AttendanceCorrectionRequestResponse MapCorrection(AttendanceCorrectionRequest item)
    {
        return new AttendanceCorrectionRequestResponse(
            item.Id,
            item.AttendanceRecordId,
            item.RequestedByUserId,
            item.RequestedByUser.DisplayName,
            item.AttendanceRecord.AttendanceDate,
            item.RequestType,
            item.RequestedClockInAt,
            item.RequestedClockOutAt,
            item.RequestedNotes,
            item.Reason,
            item.Status,
            item.CreatedAt,
            item.ReviewedByUserId,
            item.ReviewedByUser?.DisplayName,
            item.ReviewedAt,
            item.ReviewerNote);
    }

    private static string NormalizeRequestType(string? requestType)
    {
        if (string.IsNullOrWhiteSpace(requestType))
        {
            return "Correction";
        }

        return requestType.Trim() switch
        {
            "MissedPunch" => "MissedPunch",
            "MissedClockIn" => "MissedPunch",
            "MissedClockOut" => "MissedPunch",
            _ => "Correction"
        };
    }
}
