using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.Attendance;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Contracts.Attendance;
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
        var existing = await LoadAttendanceRecordAsync(user.Id, today, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("You have already clocked in for today.");
        }

        var now = DateTimeOffset.UtcNow;
        var settings = await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
        var networkResult = await networkValidationService.ValidateAsync(user.BranchId, GetIpAddress(), cancellationToken);
        var lateMinutes = CalculateLateMinutes(now, settings);

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
        record.TotalWorkMinutes = (int)Math.Round((now - record.ClockInAt.Value).TotalMinutes, MidpointRounding.AwayFromZero);
        record.ClockOutIp = GetIpAddress();
        record.ClockOutUserAgent = GetUserAgent();
        record.ClockOutDeviceSummary = GetUserAgent();
        record.ClockOutNetworkValidation = networkResult.Status;
        record.ClockOutNetworkRuleId = networkResult.MatchedRuleId;
        record.Status = record.IsLate ? "Late" : "Present";
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

    public async Task<AttendanceRecordResponse> CorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var record = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == attendanceId, cancellationToken)
            ?? throw new InvalidOperationException("Attendance record not found.");

        if (request.ClockInAt is not null && request.ClockOutAt is not null && request.ClockOutAt <= request.ClockInAt)
        {
            throw new InvalidOperationException("Clock-out must be after clock-in.");
        }

        var oldValues =
            $"ClockInAt={record.ClockInAt:o};ClockOutAt={record.ClockOutAt:o};Status={record.Status};Minutes={record.TotalWorkMinutes};Notes={record.Notes}";

        record.ClockInAt = request.ClockInAt?.ToUniversalTime();
        record.ClockOutAt = request.ClockOutAt?.ToUniversalTime();
        record.Notes = request.Notes;
        ApplyDerivedAttendanceState(record, request.Status, await GetCompanySettingsAsync(cancellationToken));
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

    public async Task<AttendanceRecordResponse> SelfCorrectAttendanceAsync(Guid attendanceId, AttendanceCorrectionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentUserEntityAsync(cancellationToken);
        var record = await dbContext.AttendanceRecords
            .Include(x => x.User)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == attendanceId && x.UserId == actor.Id, cancellationToken)
            ?? throw new InvalidOperationException("Attendance record not found.");

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
        ApplyDerivedAttendanceState(record, null, await GetCompanySettingsAsync(cancellationToken));
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

    private static int CalculateLateMinutes(DateTimeOffset now, CompanySetting settings)
    {
        var localNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, TimezoneId);
        var scheduled = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, settings.WorkingDayStartTime.Hour, settings.WorkingDayStartTime.Minute, 0, localNow.Offset)
            .AddMinutes(settings.LateGraceMinutes);

        return localNow <= scheduled ? 0 : (int)Math.Round((localNow - scheduled).TotalMinutes, MidpointRounding.AwayFromZero);
    }

    private async Task<CompanySetting> GetCompanySettingsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
    }

    private static void ApplyDerivedAttendanceState(AttendanceRecord record, string? requestedStatus, CompanySetting settings)
    {
        record.TotalWorkMinutes = record.ClockInAt is not null && record.ClockOutAt is not null
            ? (int)Math.Round((record.ClockOutAt.Value - record.ClockInAt.Value).TotalMinutes, MidpointRounding.AwayFromZero)
            : null;

        var lateMinutes = record.ClockInAt is not null
            ? CalculateLateMinutesForInstant(record.ClockInAt.Value, settings)
            : 0;

        record.IsLate = lateMinutes > 0;
        record.LateMinutes = lateMinutes > 0 ? lateMinutes : null;

        if (!string.IsNullOrWhiteSpace(requestedStatus))
        {
            record.Status = requestedStatus;
            return;
        }

        record.Status = record.ClockOutAt is null
            ? "PendingClockOut"
            : record.IsLate ? "Late" : "Present";
    }

    private static int CalculateLateMinutesForInstant(DateTimeOffset instant, CompanySetting settings)
    {
        var localTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(instant, TimezoneId);
        var scheduled = new DateTimeOffset(
            localTime.Year,
            localTime.Month,
            localTime.Day,
            settings.WorkingDayStartTime.Hour,
            settings.WorkingDayStartTime.Minute,
            0,
            localTime.Offset).AddMinutes(settings.LateGraceMinutes);

        return localTime <= scheduled ? 0 : (int)Math.Round((localTime - scheduled).TotalMinutes, MidpointRounding.AwayFromZero);
    }

    private static DateOnly GetBusinessDate() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, TimezoneId));

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
}
