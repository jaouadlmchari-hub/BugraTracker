using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

public class CanChangeIssueStatusHandler: AuthorizationHandler<CanChangeIssueStatusRequirement, IssueDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IProjectMemberService _projectMemberService;

    public CanChangeIssueStatusHandler(
        ICurrentUserService currentUserService,
        IProjectMemberService projectMemberService)
    {
        _currentUserService = currentUserService;
        _projectMemberService = projectMemberService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanChangeIssueStatusRequirement requirement,
        IssueDto issue)
    {
        var userId = _currentUserService.UserId;

        // 1. System Admin
        if (_currentUserService.IsAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Vérifier que l'utilisateur est membre du projet
        var member = await _projectMemberService.GetMemberAsync(
            issue.ProjectId,
            userId);

        if (member == null)
            return;

        // 3. Project Manager
        if (member.Role == ProjectRole.Manager)
        {
            context.Succeed(requirement);
            return;
        }

        // 4. Assignee de l'Issue
        if (issue.AssigneeId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // 5. QA uniquement pour les Bugs
        if (member.Role == ProjectRole.QA &&
            issue.Type == IssueType.Bug)
        {
            context.Succeed(requirement);
        }
    }
}