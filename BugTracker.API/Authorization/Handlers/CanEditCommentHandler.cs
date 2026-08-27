using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace BugTracker.API.Authorization.Handlers
{
    public class CanEditCommentHandler: AuthorizationHandler<CanEditCommentRequirement, CommentDto>
    {
        private readonly ICurrentUserService _currentUserService;

        public CanEditCommentHandler(
            ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanEditCommentRequirement requirement,
            CommentDto comment)
        {
            if (comment.AuthorId == _currentUserService.UserId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
