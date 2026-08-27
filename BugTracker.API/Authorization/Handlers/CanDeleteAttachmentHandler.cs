using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace BugTracker.API.Authorization.Handlers
{
    public class CanDeleteAttachmentHandler
     : AuthorizationHandler<CanDeleteAttachmentRequirement, AttachmentDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIssueService _issueService;
        private readonly IProjectMemberService _projectMemberService;

        public CanDeleteAttachmentHandler(
            ICurrentUserService currentUserService,
            IIssueService issueService,
            IProjectMemberService projectMemberService)
        {
            _currentUserService = currentUserService;
            _issueService = issueService;
            _projectMemberService = projectMemberService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanDeleteAttachmentRequirement requirement,
            AttachmentDto attachment)
        {
            var userId = _currentUserService.UserId;

            // System Admin
            if (_currentUserService.IsAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Uploader
            if (attachment.UploaderId == userId)
            {
                context.Succeed(requirement);
                return;
            }

            // Récupérer le projet via l'Issue
            var issue = await _issueService.GetByIdAsync(
                attachment.IssueId);

            if (issue == null)
                return;

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
