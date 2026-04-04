namespace TravelPax.Workforce.Contracts.Leave;

public sealed record LeaveRequestCreateRequest(
    string LeaveType,
    string DayPortion,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);

public sealed record LeaveRequestReviewRequest(
    bool Approve,
    string? ReviewerNote);

public sealed record LeaveRequestResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    string Department,
    string LeaveType,
    string DayPortion,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? ReviewedByUserId,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt,
    string? ReviewerNote,
    Guid? HrReviewedByUserId,
    string? HrReviewedByName,
    DateTimeOffset? HrReviewedAt,
    string? HrReviewerNote,
    Guid? DirectorReviewedByUserId,
    string? DirectorReviewedByName,
    DateTimeOffset? DirectorReviewedAt,
    string? DirectorReviewerNote);

public sealed record LeaveRequestListResponse(
    IReadOnlyCollection<LeaveRequestResponse> Items,
    int TotalCount);

public sealed record LeaveBalanceResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string EmployeeId,
    int Year,
    string LeaveType,
    decimal AllocatedDays,
    decimal CarryForwardDays,
    decimal ManualAdjustmentDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays);

public sealed record LeavePolicyResponse(
    Guid Id,
    string LeaveType,
    string EmploymentType,
    Guid? BranchId,
    string? BranchName,
    int AnnualAllocationDays,
    int MaxCarryForwardDays,
    bool IsActive);

public sealed record LeavePolicyUpsertRequest(
    string LeaveType,
    string EmploymentType,
    Guid? BranchId,
    int AnnualAllocationDays,
    int MaxCarryForwardDays,
    bool IsActive);
