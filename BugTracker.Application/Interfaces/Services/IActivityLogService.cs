using BugTracker.Application.DTOs.ActivityLogs;
using BugTracker.Domain.Enums;

public interface IActivityLogService
{
    Task<IEnumerable<ActivityLogDto>> GetByIssueAsync(Guid issueId);

    Task LogAsync(
        Guid issueId,
        Guid userId,
        ActivityAction action,
        string? field = null,
        string? fromValue = null,
        string? toValue = null);
   
}