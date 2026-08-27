using BugTracker.Application.DTOs.ActivityLogs;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/activity-logs")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;
    private readonly IIssueService _issueService;
    private readonly IAuthorizationService _authorizationService;

    public ActivityLogsController(
        IActivityLogService activityLogService,
        IIssueService issueService,
        IAuthorizationService authorizationService)
    {
        _activityLogService = activityLogService;
        _issueService = issueService;
        _authorizationService = authorizationService;
    }


    [HttpGet("/api/issues/{issueId:guid}/activity-logs")]
    [ProducesResponseType(
    typeof(IEnumerable<ActivityLogDto>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetByIssue(Guid issueId)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var logs =
            await _activityLogService.GetByIssueAsync(issueId);

        return Ok(logs);
    }
}