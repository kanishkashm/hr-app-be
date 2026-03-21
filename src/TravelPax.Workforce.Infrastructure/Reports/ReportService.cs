using System.Text;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.Reports;
using TravelPax.Workforce.Contracts.Reports;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Reports;

public sealed class ReportService(TravelPaxDbContext dbContext) : IReportService
{
    public async Task<AttendanceReportResponse> GetAttendanceReportAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = BuildQuery(fromDate, toDate, branchId, department, status);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.User.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var summaryData = await BuildQuery(fromDate, toDate, branchId, department, status).ToListAsync(cancellationToken);
        var summary = BuildSummary(summaryData);

        return new AttendanceReportResponse(items.Select(MapItem).ToArray(), totalCount, summary);
    }

    public async Task<IReadOnlyCollection<AttendanceTrendPointResponse>> GetAttendanceTrendAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        CancellationToken cancellationToken = default)
    {
        var resolvedTo = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = fromDate ?? resolvedTo.AddDays(-13);

        var data = await BuildQuery(resolvedFrom, resolvedTo, branchId, department, null)
            .ToListAsync(cancellationToken);

        var trend = data
            .GroupBy(x => x.AttendanceDate)
            .OrderBy(x => x.Key)
            .Select(group => new AttendanceTrendPointResponse(
                group.Key,
                group.Count(x => x.Status == "Present"),
                group.Count(x => x.Status == "Late"),
                group.Count(x => x.Status == "PendingClockOut"),
                group.Count(x => x.ClockInAt != null)))
            .ToArray();

        return trend;
    }

    public async Task<string> ExportAttendanceCsvAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var data = await BuildQuery(fromDate, toDate, branchId, department, status)
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.User.DisplayName)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("AttendanceDate,EmployeeId,EmployeeName,Department,Designation,Branch,ClockInAt,ClockOutAt,TotalWorkMinutes,Status,IsLate,ClockInNetwork,ClockOutNetwork");

        foreach (var item in data)
        {
            builder.AppendLine(string.Join(',',
                Escape(item.AttendanceDate.ToString("yyyy-MM-dd")),
                Escape(item.User.EmployeeId),
                Escape(item.User.DisplayName),
                Escape(item.User.Department ?? string.Empty),
                Escape(item.User.Designation ?? string.Empty),
                Escape(item.Branch?.Name ?? item.User.Branch?.Name ?? string.Empty),
                Escape(item.ClockInAt?.ToString("o") ?? string.Empty),
                Escape(item.ClockOutAt?.ToString("o") ?? string.Empty),
                Escape(item.TotalWorkMinutes?.ToString() ?? string.Empty),
                Escape(item.Status),
                Escape(item.IsLate ? "Yes" : "No"),
                Escape(item.ClockInNetworkValidation ?? string.Empty),
                Escape(item.ClockOutNetworkValidation ?? string.Empty)));
        }

        return builder.ToString();
    }

    private IQueryable<AttendanceRecord> BuildQuery(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status)
    {
        var query = dbContext.AttendanceRecords
            .Include(x => x.User)
            .ThenInclude(x => x.Branch)
            .Include(x => x.Branch)
            .AsQueryable();

        if (fromDate is not null) query = query.Where(x => x.AttendanceDate >= fromDate.Value);
        if (toDate is not null) query = query.Where(x => x.AttendanceDate <= toDate.Value);
        if (branchId is not null) query = query.Where(x => x.BranchId == branchId.Value);
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(x => x.User.Department == department);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

        return query;
    }

    private static AttendanceReportSummaryResponse BuildSummary(IReadOnlyCollection<AttendanceRecord> data)
    {
        var avgWorkMinutes = data
            .Where(x => x.TotalWorkMinutes.HasValue)
            .Select(x => x.TotalWorkMinutes!.Value)
            .DefaultIfEmpty(0)
            .Average();

        return new AttendanceReportSummaryResponse(
            data.Count,
            data.Count(x => x.Status == "Present"),
            data.Count(x => x.Status == "Late"),
            data.Count(x => x.Status == "PendingClockOut"),
            data.Select(x => x.UserId).Distinct().Count(),
            Math.Round(avgWorkMinutes / 60d, 2));
    }

    private static AttendanceReportItemResponse MapItem(AttendanceRecord record)
    {
        return new AttendanceReportItemResponse(
            record.Id,
            record.AttendanceDate,
            record.UserId,
            record.User.EmployeeId,
            record.User.DisplayName,
            record.User.Department ?? string.Empty,
            record.User.Designation ?? string.Empty,
            record.Branch?.Name ?? record.User.Branch?.Name ?? string.Empty,
            record.ClockInAt,
            record.ClockOutAt,
            record.TotalWorkMinutes,
            record.Status,
            record.IsLate,
            record.ClockInNetworkValidation,
            record.ClockOutNetworkValidation);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
