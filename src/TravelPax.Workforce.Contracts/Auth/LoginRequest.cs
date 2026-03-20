namespace TravelPax.Workforce.Contracts.Auth;

public sealed record LoginRequest(string EmailOrUsername, string Password);
