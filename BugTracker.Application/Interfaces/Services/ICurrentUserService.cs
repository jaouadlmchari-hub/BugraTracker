namespace BugTracker.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }
}