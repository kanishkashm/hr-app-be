using TravelPax.Workforce.Contracts.Shifts;

namespace TravelPax.Workforce.Application.Abstractions.Shifts;

public interface IShiftService
{
    Task<IReadOnlyCollection<ShiftResponse>> GetShiftsAsync(Guid? branchId, bool? isActive, CancellationToken cancellationToken = default);
    Task<ShiftResponse> CreateShiftAsync(UpsertShiftRequest request, CancellationToken cancellationToken = default);
    Task<ShiftResponse> UpdateShiftAsync(Guid shiftId, UpsertShiftRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShiftAssignmentResponse>> GetAssignmentsAsync(Guid? userId, Guid? shiftId, bool? isActive, CancellationToken cancellationToken = default);
    Task<ShiftAssignmentResponse> CreateAssignmentAsync(UpsertShiftAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<ShiftAssignmentResponse> UpdateAssignmentAsync(Guid assignmentId, UpsertShiftAssignmentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShiftAssignmentRuleResponse>> GetAssignmentRulesAsync(Guid? branchId, string? department, bool? isActive, CancellationToken cancellationToken = default);
    Task<ShiftAssignmentRuleResponse> CreateAssignmentRuleAsync(UpsertShiftAssignmentRuleRequest request, CancellationToken cancellationToken = default);
    Task<ShiftAssignmentRuleResponse> UpdateAssignmentRuleAsync(Guid ruleId, UpsertShiftAssignmentRuleRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShiftOverrideResponse>> GetOverridesAsync(Guid? userId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<ShiftOverrideResponse> CreateOverrideAsync(UpsertShiftOverrideRequest request, CancellationToken cancellationToken = default);
    Task<ShiftOverrideResponse> UpdateOverrideAsync(Guid overrideId, UpsertShiftOverrideRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShiftCalendarItemResponse>> GetCalendarAsync(DateOnly fromDate, DateOnly toDate, Guid? branchId, Guid? userId, CancellationToken cancellationToken = default);
}

