using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Managers;
using TravelPax.Workforce.Contracts.Managers;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.Infrastructure.Managers;

public sealed class ManagerService(
    TravelPaxDbContext dbContext,
    ICurrentUserService currentUserService) : IManagerService
{
    public async Task<ManagerDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var managerId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var today = GetBusinessDate();
        var teamUsers = await dbContext.Users
            .Include(x => x.Branch)
            .Where(x => x.ReportingManagerId == managerId && x.Status == "Active")
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var teamUserIds = teamUsers.Select(x => x.Id).ToArray();
        var attendanceRecords = teamUserIds.Length == 0
            ? new List<Domain.Entities.AttendanceRecord>()
            : await dbContext.AttendanceRecords
                .Where(x => x.AttendanceDate == today && teamUserIds.Contains(x.UserId))
                .ToListAsync(cancellationToken);

        var attendanceByUser = attendanceRecords.ToDictionary(x => x.UserId, x => x);
        var presentToday = attendanceRecords.Count(x => x.ClockInAt is not null);
        var lateToday = attendanceRecords.Count(x => x.IsLate);
        var absentOrNotClocked = Math.Max(teamUsers.Count - presentToday, 0);

        var pendingAttendanceApprovals = teamUserIds.Length == 0
            ? 0
            : await dbContext.AttendanceCorrectionRequests.CountAsync(
                x => x.Status == "Pending" && teamUserIds.Contains(x.RequestedByUserId),
                cancellationToken);
        var pendingLeaveApprovals = teamUserIds.Length == 0
            ? 0
            : await dbContext.LeaveRequests.CountAsync(
                x => x.Status == "Pending" && teamUserIds.Contains(x.UserId),
                cancellationToken);
        var pendingProfileApprovals = teamUserIds.Length == 0
            ? 0
            : await dbContext.UserProfileUpdateRequests.CountAsync(
                x => x.Status == "Pending" && teamUserIds.Contains(x.UserId),
                cancellationToken);

        var teamToday = teamUsers.Select(user =>
        {
            var attendance = attendanceByUser.GetValueOrDefault(user.Id);
            return new ManagerTeamAttendanceItemResponse(
                user.Id,
                user.DisplayName,
                user.EmployeeId,
                user.Department ?? string.Empty,
                user.Designation ?? string.Empty,
                user.Branch?.Name ?? string.Empty,
                attendance?.Status ?? "NotClocked",
                attendance?.ClockInAt,
                attendance?.ClockOutAt);
        }).ToArray();

        return new ManagerDashboardResponse(
            teamUsers.Count,
            presentToday,
            lateToday,
            absentOrNotClocked,
            pendingAttendanceApprovals,
            pendingLeaveApprovals,
            pendingProfileApprovals,
            teamToday);
    }

    private static DateOnly GetBusinessDate()
    {
        var local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Colombo");
        return DateOnly.FromDateTime(local);
    }
}
