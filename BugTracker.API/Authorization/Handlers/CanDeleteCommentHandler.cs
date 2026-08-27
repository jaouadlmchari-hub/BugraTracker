using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace BugTracker.API.Authorization.Handlers
{
    public class CanDeleteCommentHandler : AuthorizationHandler<CanDeleteCommentRequirement, CommentDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIssueService _issueService;
        private readonly IProjectMemberService _projectMemberService;

        public CanDeleteCommentHandler(
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
            CanDeleteCommentRequirement requirement,
            CommentDto comment)
        {
            var userId = _currentUserService.UserId;

            // Admin système
            if (_currentUserService.IsAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Auteur
            if (comment.AuthorId == userId)
            {
                context.Succeed(requirement);
                return;
            }

            var issue = await _issueService.GetByIdAsync(
                comment.IssueId);

            if (issue == null)
                return;

            // Manager du projet
            var isManager =
                await _projectMemberService.IsManagerAsync(
                    issue.ProjectId,
                    userId);

            if (isManager)
                context.Succeed(requirement);
        }
    }
}
