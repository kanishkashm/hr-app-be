using Microsoft.AspNetCore.Http;
using TravelPax.Workforce.Contracts.Attendance;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Attendance;

namespace TravelPax.Workforce.UnitTests;

public sealed class AttendanceWorkflowIntegrationTests
{
    [Fact]
    public async Task SubmitAndApproveCorrectionRequest_UpdatesAttendanceRecord()
    {
        await using var db = TestDbFactory.CreateContext();
        var branchId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var attendanceId = Guid.NewGuid();
        var attendanceDate = new DateOnly(2026, 3, 20);

        db.CompanySettings.Add(new CompanySetting
        {
            Id = Guid.NewGuid(),
            CompanyName = "TravelPax",
            DefaultTimezone = "Asia/Colombo",
            WorkingDayStartTime = new TimeOnly(9, 0),
            WorkingDayEndTime = new TimeOnly(18, 0),
            LateGraceMinutes = 15,
            WeekendConfig = "{\"days\":[\"Saturday\",\"Sunday\"]}",
        });

        db.OfficeBranches.Add(new OfficeBranch
        {
            Id = branchId,
            Code = "CMB-HQ",
            Name = "Colombo Head Office",
            Timezone = "Asia/Colombo",
        });

        db.Users.Add(new AppUser
        {
            Id = employeeId,
            EmployeeId = "TP-0003",
            FirstName = "TravelPax",
            LastName = "Employee",
            DisplayName = "TravelPax Employee",
            UserName = "employee",
            NormalizedUserName = "EMPLOYEE",
            Email = "employee@travelpax.lk",
            NormalizedEmail = "EMPLOYEE@TRAVELPAX.LK",
            BranchId = branchId,
            Status = "Active",
        });
        db.Users.Add(new AppUser
        {
            Id = reviewerId,
            EmployeeId = "TP-0002",
            FirstName = "HR",
            LastName = "Admin",
            DisplayName = "HR Admin",
            UserName = "hradmin",
            NormalizedUserName = "HRADMIN",
            Email = "hradmin@travelpax.lk",
            NormalizedEmail = "HRADMIN@TRAVELPAX.LK",
            BranchId = branchId,
            Status = "Active",
        });

        db.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = attendanceId,
            UserId = employeeId,
            BranchId = branchId,
            AttendanceDate = attendanceDate,
            ClockInAt = new DateTimeOffset(2026, 3, 20, 4, 30, 0, TimeSpan.Zero),
            ClockOutAt = new DateTimeOffset(2026, 3, 20, 11, 0, 0, TimeSpan.Zero),
            TotalWorkMinutes = 390,
            Status = "HalfDay",
            CreatedBy = employeeId,
            UpdatedBy = employeeId,
        });

        await db.SaveChangesAsync();

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        httpContextAccessor.HttpContext!.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("112.134.207.60");

        var employeeService = new AttendanceService(
            db,
            new FakeCurrentUserService(employeeId),
            httpContextAccessor,
            new FakeNetworkValidationService(new("InsideOfficeNetwork", null)));

        var submitted = await employeeService.SubmitCorrectionRequestAsync(
            attendanceId,
            new AttendanceCorrectionSubmissionRequest(
                ClockInAt: new DateTimeOffset(2026, 3, 20, 4, 0, 0, TimeSpan.Zero),
                ClockOutAt: new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
                Notes: "Corrected via ESS",
                Reason: "Missed exact punch due to browser refresh",
                RequestType: "Correction"));

        Assert.Equal("Pending", submitted.Status);

        var reviewerService = new AttendanceService(
            db,
            new FakeCurrentUserService(reviewerId),
            httpContextAccessor,
            new FakeNetworkValidationService(new("InsideOfficeNetwork", null)));

        var reviewed = await reviewerService.ReviewCorrectionRequestAsync(
            submitted.Id,
            new AttendanceCorrectionReviewRequest(true, "Approved after verification"));

        Assert.Equal("Approved", reviewed.Status);

        var updated = await db.AttendanceRecords.FindAsync(attendanceId);
        Assert.NotNull(updated);
        Assert.Equal(new DateTimeOffset(2026, 3, 20, 4, 0, 0, TimeSpan.Zero), updated!.ClockInAt);
        Assert.Equal(new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero), updated.ClockOutAt);
        Assert.True(updated.TotalWorkMinutes >= 480);
    }
}
