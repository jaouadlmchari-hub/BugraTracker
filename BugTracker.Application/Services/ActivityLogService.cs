using BugTracker.Application.DTOs.ActivityLogs;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

public class ActivityLogService : IActivityLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivityLogService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(
        Guid issueId,
        Guid userId,
        ActivityAction action,
        string? field = null,
        string? fromValue = null,
        string? toValue = null)
    {
        var activityLog = new ActivityLog
        {
            IssueId = issueId,
            UserId = userId,
            Action = action,
            Field = field,
            FromValue = fromValue,
            ToValue = toValue
        };

        await _unitOfWork.ActivityLogs.AddAsync(activityLog);
    }

    public async Task<IEnumerable<ActivityLogDto>> GetByIssueAsync(Guid issueId)
    {
        var issue = await _unitOfWork.Issues
            .GetByIdAsync(issueId);

        if (issue == null)
            throw new NotFoundException(
                "Issue non trouvée.");

        var logs = await _unitOfWork.ActivityLogs
            .GetByIssueIdAsync(issueId);

        return logs
            .Select(log => log.ToDto())
            .ToList();
    }
}