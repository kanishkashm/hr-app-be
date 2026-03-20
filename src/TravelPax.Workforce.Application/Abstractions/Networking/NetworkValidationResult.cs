namespace TravelPax.Workforce.Application.Abstractions.Networking;

public sealed record NetworkValidationResult(
    string Status,
    Guid? MatchedRuleId);
