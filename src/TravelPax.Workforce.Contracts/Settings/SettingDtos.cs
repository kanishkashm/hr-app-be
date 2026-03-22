namespace TravelPax.Workforce.Contracts.Settings;

public sealed record CompanySettingResponse(
    Guid Id,
    string CompanyName,
    string DefaultTimezone,
    string WorkingDayStartTime,
    string WorkingDayEndTime,
    int LateGraceMinutes,
    string WeekendConfig);

public sealed record UpdateCompanySettingRequest(
    string CompanyName,
    string DefaultTimezone,
    string WorkingDayStartTime,
    string WorkingDayEndTime,
    int LateGraceMinutes,
    string WeekendConfig);

public sealed record BranchResponse(
    Guid Id,
    string Code,
    string Name,
    string? City,
    string Country,
    string Timezone,
    bool IsActive);

public sealed record UpsertBranchRequest(
    string Code,
    string Name,
    string? City,
    string Country,
    string Timezone,
    bool IsActive);

public sealed record AllowedNetworkResponse(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Name,
    string NetworkType,
    string IpOrCidr,
    string ValidationMode,
    bool IsActive,
    int Priority);

public sealed record UpsertAllowedNetworkRequest(
    Guid BranchId,
    string Name,
    string NetworkType,
    string IpOrCidr,
    string ValidationMode,
    bool IsActive,
    int Priority);

public sealed record SettingsOverviewResponse(
    CompanySettingResponse Company,
    IReadOnlyCollection<BranchResponse> Branches,
    IReadOnlyCollection<AllowedNetworkResponse> Networks);

public sealed record NetworkValidationCheckRequest(
    Guid BranchId,
    string IpAddress);

public sealed record NetworkValidationCheckResponse(
    Guid BranchId,
    string IpAddress,
    string Status,
    string? MatchedRuleName,
    string? MatchedRuleIpOrCidr);

public sealed record AttendancePeriodLockResponse(
    Guid Id,
    int Year,
    int Month,
    Guid? BranchId,
    string BranchName,
    bool IsLocked,
    DateTimeOffset LockedAt,
    Guid? LockedByUserId,
    DateTimeOffset? UnlockedAt,
    Guid? UnlockedByUserId,
    string? Notes);

public sealed record SetAttendancePeriodLockRequest(
    int Year,
    int Month,
    Guid? BranchId,
    bool IsLocked,
    string? Notes);

public sealed record FinalizePayrollPeriodRequest(
    int Year,
    int Month,
    Guid? BranchId,
    string? Notes);

public sealed record PayrollPeriodFinalizationResponse(
    Guid Id,
    int Year,
    int Month,
    Guid? BranchId,
    string BranchName,
    bool IsFinalized,
    DateTimeOffset FinalizedAt,
    Guid? FinalizedByUserId,
    DateTimeOffset? ReopenedAt,
    Guid? ReopenedByUserId,
    string SnapshotJson,
    string? Notes);
