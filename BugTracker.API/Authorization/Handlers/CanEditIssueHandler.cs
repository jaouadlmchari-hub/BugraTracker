using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace BugTracker.API.Authorization.Handlers
{
    public class CanEditIssueHandler : AuthorizationHandler<CanEditIssueRequirement, IssueDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectMemberService _projectMemberService;

        public CanEditIssueHandler(
            ICurrentUserService currentUserService,
            IProjectMemberService projectMemberService)
        {
            _currentUserService = currentUserService;
            _projectMemberService = projectMemberService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanEditIssueRequirement requirement,
            IssueDto issue)
        {
            var userId = _currentUserService.UserId;

            // System Admin
            if (_currentUserService.IsAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Reporter ou Assignee
            if (issue.ReporterId == userId ||
                issue.AssigneeId == userId)
            {
                context.Succeed(requirement);
                return;
            }

            // Project Manager
            var isManager =
                await _projectMemberService.IsManagerAsync(
                    issue.ProjectId,
                    userId);

            if (isManager)
                context.Succeed(requirement);
        }
    }
}
