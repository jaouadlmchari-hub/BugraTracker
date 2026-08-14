using BugTracker.Domain.Enums;

public interface IActivityLogService
{
    Task LogAsync(
        Guid issueId,
        Guid userId,
        ActivityAction action,
        string? field = null,
        string? fromValue = null,
        string? toValue = null);
}