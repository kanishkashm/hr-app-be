using TravelPax.Workforce.Domain.Entities;

namespace TravelPax.Workforce.Infrastructure.Attendance;

internal static class AttendanceRulesEngine
{
    internal const string DefaultTimezone = "Asia/Colombo";

    internal sealed record EffectiveAttendanceRules(
        int LateGraceMinutes,
        int HalfDayThresholdMinutes,
        int MinPresentMinutes,
        int OvertimeThresholdMinutes,
        int EarlyOutGraceMinutes,
        int ShortLeaveDeductionMinutes,
        bool EnableMissedPunchDetection);

    internal sealed record AttendanceRuleComputation(
        string SuggestedStatus,
        int? TotalWorkMinutes,
        bool IsLate,
        int LateMinutes,
        bool IsEarlyOut,
        int EarlyOutMinutes,
        bool IsOvertime,
        int OvertimeMinutes,
        bool IsMissedPunch);

    internal static AttendanceRuleProfile? ResolveBestProfile(
        IEnumerable<AttendanceRuleProfile> profiles,
        Guid? branchId,
        Guid? shiftId)
    {
        return profiles
            .Where(x => x.IsActive)
            .Where(x => MatchesScope(x, branchId, shiftId))
            .OrderBy(x => ScopeRank(x.ScopeType))
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefault();
    }

    internal static EffectiveAttendanceRules BuildEffectiveRules(
        CompanySetting settings,
        ShiftDefinition? shift,
        AttendanceRuleProfile? profile)
    {
        return new EffectiveAttendanceRules(
            LateGraceMinutes: Math.Max(profile?.LateGraceMinutes ?? shift?.GraceMinutes ?? settings.LateGraceMinutes, 0),
            HalfDayThresholdMinutes: Math.Max(profile?.HalfDayThresholdMinutes ?? shift?.MinHalfDayMinutes ?? 240, 0),
            MinPresentMinutes: Math.Max(profile?.MinPresentMinutes ?? shift?.MinPresentMinutes ?? 480, 0),
            OvertimeThresholdMinutes: Math.Max(profile?.OvertimeThresholdMinutes ?? shift?.OvertimeAfterMinutes ?? 540, 0),
            EarlyOutGraceMinutes: Math.Max(profile?.EarlyOutGraceMinutes ?? 0, 0),
            ShortLeaveDeductionMinutes: Math.Max(profile?.ShortLeaveDeductionMinutes ?? 0, 0),
            EnableMissedPunchDetection: profile?.EnableMissedPunchDetection ?? true);
    }

    internal static AttendanceRuleComputation Evaluate(
        DateOnly attendanceDate,
        DateTimeOffset? clockInAt,
        DateTimeOffset? clockOutAt,
        CompanySetting settings,
        ShiftDefinition? shift,
        EffectiveAttendanceRules rules,
        DateOnly businessDate)
    {
        var timezone = settings.DefaultTimezone ?? DefaultTimezone;
        var hasClockIn = clockInAt is not null;
        var hasClockOut = clockOutAt is not null;

        if (!hasClockIn && !hasClockOut)
        {
            if (attendanceDate < businessDate)
            {
                return new AttendanceRuleComputation("Absent", null, false, 0, false, 0, false, 0, false);
            }

            return new AttendanceRuleComputation("NotClockedIn", null, false, 0, false, 0, false, 0, false);
        }

        if (!hasClockIn && hasClockOut)
        {
            return new AttendanceRuleComputation("MissedClockIn", null, false, 0, false, 0, false, 0, true);
        }

        var lateMinutes = CalculateLateMinutes(clockInAt!.Value, settings, shift, rules, timezone);
        if (!hasClockOut)
        {
            var isMissedClockOut = rules.EnableMissedPunchDetection && attendanceDate < businessDate;
            return new AttendanceRuleComputation(
                isMissedClockOut ? "MissedClockOut" : "PendingClockOut",
                null,
                lateMinutes > 0,
                lateMinutes,
                false,
                0,
                false,
                0,
                isMissedClockOut);
        }

        var totalMinutes = (int)Math.Round((clockOutAt!.Value - clockInAt!.Value).TotalMinutes, MidpointRounding.AwayFromZero);
        totalMinutes = Math.Max(totalMinutes, 0);

        if (totalMinutes < rules.HalfDayThresholdMinutes)
        {
            return new AttendanceRuleComputation("Absent", totalMinutes, lateMinutes > 0, lateMinutes, false, 0, false, 0, false);
        }

        if (totalMinutes < rules.MinPresentMinutes)
        {
            return new AttendanceRuleComputation("HalfDay", totalMinutes, lateMinutes > 0, lateMinutes, false, 0, false, 0, false);
        }

        var earlyOutMinutes = CalculateEarlyOutMinutes(attendanceDate, clockOutAt.Value, settings, shift, rules, timezone);
        var overtimeMinutes = Math.Max(totalMinutes - rules.OvertimeThresholdMinutes, 0);
        var isEarlyOut = earlyOutMinutes > 0;
        var isOvertime = overtimeMinutes > 0;

        var status = lateMinutes > 0
            ? "Late"
            : isEarlyOut
                ? "EarlyOut"
                : isOvertime
                    ? "Overtime"
                    : "Present";

        return new AttendanceRuleComputation(status, totalMinutes, lateMinutes > 0, lateMinutes, isEarlyOut, earlyOutMinutes, isOvertime, overtimeMinutes, false);
    }

    private static bool MatchesScope(AttendanceRuleProfile profile, Guid? branchId, Guid? shiftId)
    {
        if (string.Equals(profile.ScopeType, "Shift", StringComparison.OrdinalIgnoreCase))
        {
            return shiftId is not null && profile.ShiftId == shiftId;
        }

        if (string.Equals(profile.ScopeType, "Branch", StringComparison.OrdinalIgnoreCase))
        {
            return branchId is not null && profile.BranchId == branchId;
        }

        return string.Equals(profile.ScopeType, "Global", StringComparison.OrdinalIgnoreCase);
    }

    private static int ScopeRank(string scopeType)
    {
        return scopeType.ToLowerInvariant() switch
        {
            "shift" => 0,
            "branch" => 1,
            _ => 2
        };
    }

    private static int CalculateLateMinutes(
        DateTimeOffset clockInAt,
        CompanySetting settings,
        ShiftDefinition? shift,
        EffectiveAttendanceRules rules,
        string timezone)
    {
        var local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clockInAt, timezone);
        var start = shift?.StartTime ?? settings.WorkingDayStartTime;
        var grace = rules.LateGraceMinutes;

        if (string.Equals(shift?.ShiftType, "Flexible", StringComparison.OrdinalIgnoreCase))
        {
            grace += Math.Max(shift?.FlexMinutes ?? 0, 0);
        }

        var scheduled = new DateTimeOffset(local.Year, local.Month, local.Day, start.Hour, start.Minute, 0, local.Offset).AddMinutes(grace);
        return local <= scheduled ? 0 : (int)Math.Round((local - scheduled).TotalMinutes, MidpointRounding.AwayFromZero);
    }

    private static int CalculateEarlyOutMinutes(
        DateOnly attendanceDate,
        DateTimeOffset clockOutAt,
        CompanySetting settings,
        ShiftDefinition? shift,
        EffectiveAttendanceRules rules,
        string timezone)
    {
        var localClockOut = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clockOutAt, timezone);
        var start = shift?.StartTime ?? settings.WorkingDayStartTime;
        var end = shift?.EndTime ?? settings.WorkingDayEndTime;
        var isOvernight = end <= start;

        var scheduledEndDate = isOvernight ? attendanceDate.AddDays(1) : attendanceDate;
        var scheduledEnd = new DateTimeOffset(
            scheduledEndDate.Year,
            scheduledEndDate.Month,
            scheduledEndDate.Day,
            end.Hour,
            end.Minute,
            0,
            localClockOut.Offset).AddMinutes(-rules.EarlyOutGraceMinutes);

        if (localClockOut >= scheduledEnd)
        {
            return 0;
        }

        return (int)Math.Round((scheduledEnd - localClockOut).TotalMinutes, MidpointRounding.AwayFromZero);
    }
}
