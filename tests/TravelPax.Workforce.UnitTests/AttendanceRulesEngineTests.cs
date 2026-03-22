using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Attendance;

namespace TravelPax.Workforce.UnitTests;

public sealed class AttendanceRulesEngineTests
{
    [Fact]
    public void ResolveBestProfile_PrefersShiftProfile_OverBranchAndGlobal()
    {
        var branchId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var profiles = new[]
        {
            new AttendanceRuleProfile { Id = Guid.NewGuid(), Name = "Global", ScopeType = "Global", Priority = 1, IsActive = true },
            new AttendanceRuleProfile { Id = Guid.NewGuid(), Name = "Branch", ScopeType = "Branch", BranchId = branchId, Priority = 1, IsActive = true },
            new AttendanceRuleProfile { Id = Guid.NewGuid(), Name = "Shift", ScopeType = "Shift", ShiftId = shiftId, Priority = 99, IsActive = true },
        };

        var best = AttendanceRulesEngine.ResolveBestProfile(profiles, branchId, shiftId);

        Assert.NotNull(best);
        Assert.Equal("Shift", best!.Name);
        Assert.Equal("Shift", best.ScopeType);
    }

    [Fact]
    public void Evaluate_ReturnsMissedClockOut_WhenPastDateAndClockOutMissing()
    {
        var settings = new CompanySetting
        {
            DefaultTimezone = "Asia/Colombo",
            WorkingDayStartTime = new TimeOnly(9, 0),
            WorkingDayEndTime = new TimeOnly(18, 0),
            LateGraceMinutes = 15,
        };

        var rules = AttendanceRulesEngine.BuildEffectiveRules(settings, null, new AttendanceRuleProfile
        {
            EnableMissedPunchDetection = true,
            LateGraceMinutes = 15,
            HalfDayThresholdMinutes = 240,
            MinPresentMinutes = 480,
            OvertimeThresholdMinutes = 540,
        });

        var attendanceDate = new DateOnly(2026, 3, 20);
        var businessDate = attendanceDate.AddDays(1);
        var clockInUtc = new DateTimeOffset(2026, 3, 20, 3, 30, 0, TimeSpan.Zero); // 09:00 LK

        var result = AttendanceRulesEngine.Evaluate(
            attendanceDate,
            clockInUtc,
            null,
            settings,
            null,
            rules,
            businessDate);

        Assert.Equal("MissedClockOut", result.SuggestedStatus);
        Assert.True(result.IsMissedPunch);
        Assert.Null(result.TotalWorkMinutes);
    }

    [Fact]
    public void Evaluate_ReturnsLate_WhenWorkedEnoughMinutes_AndClockInPastGrace()
    {
        var settings = new CompanySetting
        {
            DefaultTimezone = "Asia/Colombo",
            WorkingDayStartTime = new TimeOnly(9, 0),
            WorkingDayEndTime = new TimeOnly(18, 0),
            LateGraceMinutes = 10,
        };

        var rules = AttendanceRulesEngine.BuildEffectiveRules(settings, null, new AttendanceRuleProfile
        {
            LateGraceMinutes = 10,
            HalfDayThresholdMinutes = 240,
            MinPresentMinutes = 480,
            OvertimeThresholdMinutes = 540,
            EnableMissedPunchDetection = true,
        });

        var attendanceDate = new DateOnly(2026, 3, 20);
        var businessDate = attendanceDate;
        var clockInUtc = new DateTimeOffset(2026, 3, 20, 4, 0, 0, TimeSpan.Zero); // 09:30 LK (20 min late after grace)
        var clockOutUtc = new DateTimeOffset(2026, 3, 20, 12, 30, 0, TimeSpan.Zero); // 18:00 LK

        var result = AttendanceRulesEngine.Evaluate(
            attendanceDate,
            clockInUtc,
            clockOutUtc,
            settings,
            null,
            rules,
            businessDate);

        Assert.Equal("Late", result.SuggestedStatus);
        Assert.True(result.IsLate);
        Assert.True(result.LateMinutes > 0);
        Assert.NotNull(result.TotalWorkMinutes);
    }
}
