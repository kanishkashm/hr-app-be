namespace TravelPax.Workforce.Contracts.Shifts;

public sealed record ShiftResponse(
    Guid Id,
    string Code,
    string Name,
    Guid? BranchId,
    string BranchName,
    string ShiftType,
    string StartTime,
    string EndTime,
    int FlexMinutes,
    int GraceMinutes,
    int MinHalfDayMinutes,
    int MinPresentMinutes,
    int OvertimeAfterMinutes,
    bool IsActive);

public sealed record UpsertShiftRequest(
    string Code,
    string Name,
    Guid? BranchId,
    string ShiftType,
    string StartTime,
    string EndTime,
    int FlexMinutes,
    int GraceMinutes,
    int MinHalfDayMinutes,
    int MinPresentMinutes,
    int OvertimeAfterMinutes,
    bool IsActive);

public sealed record ShiftAssignmentResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    Guid ShiftId,
    string ShiftName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);

public sealed record UpsertShiftAssignmentRequest(
    Guid UserId,
    Guid ShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);

public sealed record ShiftAssignmentRuleResponse(
    Guid Id,
    Guid? BranchId,
    string BranchName,
    string? Department,
    string? Team,
    Guid ShiftId,
    string ShiftName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int Priority,
    bool IsActive);

public sealed record UpsertShiftAssignmentRuleRequest(
    Guid? BranchId,
    string? Department,
    string? Team,
    Guid ShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int Priority,
    bool IsActive);

public sealed record ShiftOverrideResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    DateOnly Date,
    Guid ShiftId,
    string ShiftName,
    string Reason,
    string Status);

public sealed record UpsertShiftOverrideRequest(
    Guid UserId,
    DateOnly Date,
    Guid ShiftId,
    string Reason,
    string Status);

public sealed record ShiftCalendarItemResponse(
    DateOnly Date,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    Guid ShiftId,
    string ShiftName,
    string ShiftType,
    string StartTime,
    string EndTime,
    int GraceMinutes,
    int FlexMinutes);

