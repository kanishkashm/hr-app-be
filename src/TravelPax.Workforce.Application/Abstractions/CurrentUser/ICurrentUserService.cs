namespace TravelPax.Workforce.Application.Abstractions.CurrentUser;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
