using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Repositories;
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
}