using BugTracker.Application.DTOs.ActivityLogs;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(
        IUnitOfWork unitOfWork,
        ILogger<ActivityLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid issueId,
        Guid userId,
        ActivityAction action,
        string? field = null,
        string? fromValue = null,
        string? toValue = null)
    {
        _logger.LogDebug(
            "Creating activity log. IssueId: {IssueId}, UserId: {UserId}, Action: {Action}, Field: {Field}",
            issueId,
            userId,
            action,
            field);

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

        _logger.LogInformation(
            "Activity log created. IssueId: {IssueId}, UserId: {UserId}, Action: {Action}",
            issueId,
            userId,
            action);
    }

    public async Task<IEnumerable<ActivityLogDto>> GetByIssueAsync(Guid issueId)
    {
        _logger.LogDebug(
            "Retrieving activity logs for IssueId: {IssueId}",
            issueId);

        var issue = await _unitOfWork.Issues
            .GetByIdAsync(issueId);

        if (issue == null)
        {
            _logger.LogWarning(
                "Cannot retrieve activity logs. Issue not found. IssueId: {IssueId}",
                issueId);

            throw new NotFoundException(
                "Issue non trouvée.");
        }

        var logs = await _unitOfWork.ActivityLogs
            .GetByIssueIdAsync(issueId);

        var result = logs
            .Select(log => log.ToDto())
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} activity logs for IssueId: {IssueId}",
            result.Count,
            issueId);

        return result;
    }
}