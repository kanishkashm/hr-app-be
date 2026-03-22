using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
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
        string? status,
        CancellationToken cancellationToken = default)
    {
        var resolvedTo = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = fromDate ?? resolvedTo.AddDays(-13);

        var data = await BuildQuery(resolvedFrom, resolvedTo, branchId, department, status)
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
        builder.AppendLine("AttendanceDate,EmployeeId,EmployeeName,Department,Designation,Branch,ClockInAt,ClockOutAt,TotalWorkMinutes,Status,IsLate,LateMinutes,ClockInIp,ClockOutIp,ClockInNetwork,ClockOutNetwork,ClockInDevice,ClockOutDevice,ClockInUserAgent,ClockOutUserAgent,Notes");

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
                Escape(item.LateMinutes?.ToString() ?? string.Empty),
                Escape(item.ClockInIp ?? string.Empty),
                Escape(item.ClockOutIp ?? string.Empty),
                Escape(item.ClockInNetworkValidation ?? string.Empty),
                Escape(item.ClockOutNetworkValidation ?? string.Empty),
                Escape(item.ClockInDeviceSummary ?? string.Empty),
                Escape(item.ClockOutDeviceSummary ?? string.Empty),
                Escape(item.ClockInUserAgent ?? string.Empty),
                Escape(item.ClockOutUserAgent ?? string.Empty),
                Escape(item.Notes ?? string.Empty)));
        }

        return builder.ToString();
    }

    public async Task<byte[]> ExportAttendanceExcelAsync(
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

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Attendance Report");

        var headers = new[]
        {
            "AttendanceDate", "EmployeeId", "EmployeeName", "Department", "Designation", "Branch",
            "ClockInAt", "ClockOutAt", "TotalWorkMinutes", "Status", "IsLate", "LateMinutes",
            "ClockInIp", "ClockOutIp", "ClockInNetwork", "ClockOutNetwork",
            "ClockInDevice", "ClockOutDevice", "ClockInUserAgent", "ClockOutUserAgent", "Notes"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.AttendanceDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 2).Value = item.User.EmployeeId;
            worksheet.Cell(row, 3).Value = item.User.DisplayName;
            worksheet.Cell(row, 4).Value = item.User.Department ?? string.Empty;
            worksheet.Cell(row, 5).Value = item.User.Designation ?? string.Empty;
            worksheet.Cell(row, 6).Value = item.Branch?.Name ?? item.User.Branch?.Name ?? string.Empty;
            worksheet.Cell(row, 7).Value = item.ClockInAt?.ToString("o") ?? string.Empty;
            worksheet.Cell(row, 8).Value = item.ClockOutAt?.ToString("o") ?? string.Empty;
            worksheet.Cell(row, 9).Value = item.TotalWorkMinutes?.ToString() ?? string.Empty;
            worksheet.Cell(row, 10).Value = item.Status;
            worksheet.Cell(row, 11).Value = item.IsLate ? "Yes" : "No";
            worksheet.Cell(row, 12).Value = item.LateMinutes?.ToString() ?? string.Empty;
            worksheet.Cell(row, 13).Value = item.ClockInIp ?? string.Empty;
            worksheet.Cell(row, 14).Value = item.ClockOutIp ?? string.Empty;
            worksheet.Cell(row, 15).Value = item.ClockInNetworkValidation ?? string.Empty;
            worksheet.Cell(row, 16).Value = item.ClockOutNetworkValidation ?? string.Empty;
            worksheet.Cell(row, 17).Value = item.ClockInDeviceSummary ?? string.Empty;
            worksheet.Cell(row, 18).Value = item.ClockOutDeviceSummary ?? string.Empty;
            worksheet.Cell(row, 19).Value = item.ClockInUserAgent ?? string.Empty;
            worksheet.Cell(row, 20).Value = item.ClockOutUserAgent ?? string.Empty;
            worksheet.Cell(row, 21).Value = item.Notes ?? string.Empty;
            row++;
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<string> ExportPayrollCsvAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildPayrollRowsAsync(fromDate, toDate, branchId, department, status, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("PeriodStart,PeriodEnd,EmployeeId,EmployeeName,Department,Designation,Branch,WorkingDays,PresentDays,LateDays,MissingPunchDays,TotalWorkHours,StandardWorkHours,OvertimeHours,PayableHours,InsideOfficeDays,OutsideOfficeDays,UnknownNetworkDays,PeriodLockStatus");

        foreach (var item in rows)
        {
            builder.AppendLine(string.Join(',',
                Escape(item.PeriodStart),
                Escape(item.PeriodEnd),
                Escape(item.EmployeeId),
                Escape(item.EmployeeName),
                Escape(item.Department),
                Escape(item.Designation),
                Escape(item.Branch),
                Escape(item.WorkingDays.ToString()),
                Escape(item.PresentDays.ToString()),
                Escape(item.LateDays.ToString()),
                Escape(item.MissingPunchDays.ToString()),
                Escape(item.TotalWorkHours.ToString("0.00")),
                Escape(item.StandardWorkHours.ToString("0.00")),
                Escape(item.OvertimeHours.ToString("0.00")),
                Escape(item.PayableHours.ToString("0.00")),
                Escape(item.InsideOfficeDays.ToString()),
                Escape(item.OutsideOfficeDays.ToString()),
                Escape(item.UnknownNetworkDays.ToString()),
                Escape(item.PeriodLockStatus)));
        }

        return builder.ToString();
    }

    public async Task<byte[]> ExportPayrollExcelAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildPayrollRowsAsync(fromDate, toDate, branchId, department, status, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Payroll Attendance");
        var headers = new[]
        {
            "PeriodStart", "PeriodEnd", "EmployeeId", "EmployeeName", "Department", "Designation", "Branch",
            "WorkingDays", "PresentDays", "LateDays", "MissingPunchDays", "TotalWorkHours", "StandardWorkHours",
            "OvertimeHours", "PayableHours", "InsideOfficeDays", "OutsideOfficeDays", "UnknownNetworkDays", "PeriodLockStatus"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        foreach (var item in rows)
        {
            worksheet.Cell(row, 1).Value = item.PeriodStart;
            worksheet.Cell(row, 2).Value = item.PeriodEnd;
            worksheet.Cell(row, 3).Value = item.EmployeeId;
            worksheet.Cell(row, 4).Value = item.EmployeeName;
            worksheet.Cell(row, 5).Value = item.Department;
            worksheet.Cell(row, 6).Value = item.Designation;
            worksheet.Cell(row, 7).Value = item.Branch;
            worksheet.Cell(row, 8).Value = item.WorkingDays;
            worksheet.Cell(row, 9).Value = item.PresentDays;
            worksheet.Cell(row, 10).Value = item.LateDays;
            worksheet.Cell(row, 11).Value = item.MissingPunchDays;
            worksheet.Cell(row, 12).Value = item.TotalWorkHours;
            worksheet.Cell(row, 13).Value = item.StandardWorkHours;
            worksheet.Cell(row, 14).Value = item.OvertimeHours;
            worksheet.Cell(row, 15).Value = item.PayableHours;
            worksheet.Cell(row, 16).Value = item.InsideOfficeDays;
            worksheet.Cell(row, 17).Value = item.OutsideOfficeDays;
            worksheet.Cell(row, 18).Value = item.UnknownNetworkDays;
            worksheet.Cell(row, 19).Value = item.PeriodLockStatus;
            row++;
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
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
            Math.Round(avgWorkMinutes / 60d, 2),
            data.Count(x => string.Equals(x.ClockInNetworkValidation, "InsideOfficeNetwork", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x.ClockOutNetworkValidation, "InsideOfficeNetwork", StringComparison.OrdinalIgnoreCase)),
            data.Count(x => string.Equals(x.ClockInNetworkValidation, "OutsideOfficeNetwork", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(x.ClockOutNetworkValidation, "InsideOfficeNetwork", StringComparison.OrdinalIgnoreCase)),
            data.Count(x => string.IsNullOrWhiteSpace(x.ClockInNetworkValidation) && string.IsNullOrWhiteSpace(x.ClockOutNetworkValidation)));
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

    private async Task<IReadOnlyCollection<PayrollExportRow>> BuildPayrollRowsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId,
        string? department,
        string? status,
        CancellationToken cancellationToken)
    {
        var resolvedTo = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = fromDate ?? new DateOnly(resolvedTo.Year, resolvedTo.Month, 1);
        var data = await BuildQuery(resolvedFrom, resolvedTo, branchId, department, status)
            .OrderBy(x => x.User.EmployeeId)
            .ThenBy(x => x.AttendanceDate)
            .ToListAsync(cancellationToken);
        var settings = await dbContext.CompanySettings.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
        var workingDays = CountWorkingDays(resolvedFrom, resolvedTo, settings.WeekendConfig);
        var dailyHours = GetStandardDailyHours(settings);
        var standardWorkHours = Math.Round(workingDays * dailyHours, 2);
        var lockLookup = await dbContext.AttendancePeriodLocks
            .Where(x => x.IsLocked
                        && (x.Year > resolvedFrom.Year || (x.Year == resolvedFrom.Year && x.Month >= resolvedFrom.Month))
                        && (x.Year < resolvedTo.Year || (x.Year == resolvedTo.Year && x.Month <= resolvedTo.Month)))
            .ToListAsync(cancellationToken);

        var rows = data
            .GroupBy(x => new
            {
                x.UserId,
                x.User.EmployeeId,
                x.User.DisplayName,
                x.User.Department,
                x.User.Designation,
                BranchName = x.Branch != null
                    ? x.Branch.Name
                    : x.User.Branch != null
                        ? x.User.Branch.Name
                        : string.Empty,
                x.BranchId
            })
            .Select(group =>
            {
                var presentDays = group.Count(x => x.ClockInAt is not null && x.ClockOutAt is not null);
                var missingPunchDays = group.Count(x => x.ClockInAt is null || x.ClockOutAt is null || x.Status == "PendingClockOut");
                var totalHours = Math.Round(group.Where(x => x.TotalWorkMinutes.HasValue).Sum(x => x.TotalWorkMinutes!.Value) / 60d, 2);
                var insideOfficeDays = group.Count(x =>
                    string.Equals(x.ClockInNetworkValidation, "InsideOfficeNetwork", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.ClockOutNetworkValidation, "InsideOfficeNetwork", StringComparison.OrdinalIgnoreCase));
                var outsideOfficeDays = group.Count(x =>
                    string.Equals(x.ClockInNetworkValidation, "OutsideOfficeNetwork", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x.ClockOutNetworkValidation, "InsideOfficeNetwork", StringComparison.OrdinalIgnoreCase));
                var unknownNetworkDays = group.Count(x => string.IsNullOrWhiteSpace(x.ClockInNetworkValidation) && string.IsNullOrWhiteSpace(x.ClockOutNetworkValidation));
                var overtimeHours = Math.Round(Math.Max(totalHours - standardWorkHours, 0), 2);
                var periodLockStatus = lockLookup.Any(x => x.BranchId == null || x.BranchId == group.Key.BranchId)
                    ? "Locked"
                    : "Unlocked";

                return new PayrollExportRow(
                    resolvedFrom.ToString("yyyy-MM-dd"),
                    resolvedTo.ToString("yyyy-MM-dd"),
                    group.Key.EmployeeId,
                    group.Key.DisplayName,
                    group.Key.Department ?? string.Empty,
                    group.Key.Designation ?? string.Empty,
                    group.Key.BranchName ?? string.Empty,
                    workingDays,
                    presentDays,
                    group.Count(x => x.IsLate),
                    missingPunchDays,
                    totalHours,
                    standardWorkHours,
                    overtimeHours,
                    totalHours,
                    insideOfficeDays,
                    outsideOfficeDays,
                    unknownNetworkDays,
                    periodLockStatus);
            })
            .ToArray();

        return rows;
    }

    private static int CountWorkingDays(DateOnly fromDate, DateOnly toDate, string? weekendConfig)
    {
        var weekendDays = ParseWeekendDays(weekendConfig);
        var holidayDates = ParseHolidayDates(weekendConfig);
        var count = 0;

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (weekendDays.Contains(date.DayOfWeek))
            {
                continue;
            }

            if (holidayDates.Contains(date))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static double GetStandardDailyHours(CompanySetting settings)
    {
        var start = settings.WorkingDayStartTime.ToTimeSpan();
        var end = settings.WorkingDayEndTime.ToTimeSpan();
        var diff = end - start;
        if (diff.TotalMinutes <= 0)
        {
            return 8d;
        }

        return Math.Round(diff.TotalHours, 2);
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
                if (dateElement.ValueKind == JsonValueKind.String && DateOnly.TryParse(dateElement.GetString(), out var parsedDate))
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

    private sealed record PayrollExportRow(
        string PeriodStart,
        string PeriodEnd,
        string EmployeeId,
        string EmployeeName,
        string Department,
        string Designation,
        string Branch,
        int WorkingDays,
        int PresentDays,
        int LateDays,
        int MissingPunchDays,
        double TotalWorkHours,
        double StandardWorkHours,
        double OvertimeHours,
        double PayableHours,
        int InsideOfficeDays,
        int OutsideOfficeDays,
        int UnknownNetworkDays,
        string PeriodLockStatus);
}
