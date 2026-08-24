using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BugTracker.API.Authorization.Handlers;

public class CanManageProjectHandler : AuthorizationHandler<CanManageProjectRequirement, Guid>
{
    private readonly IProjectMemberService _projectMemberService;

    public CanManageProjectHandler(
        IProjectMemberService projectMemberService)
    {
        _projectMemberService = projectMemberService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanManageProjectRequirement requirement,
        Guid projectId)
    {
       
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

     
        var userIdClaim = context.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdClaim, out var currentUserId))
            return;

        // Vérifier son rôle dans CE projet
        var isManager =
            await _projectMemberService.IsManagerAsync(
                projectId,
                currentUserId);

        if (isManager)
        {
            context.Succeed(requirement);
        }
    }
}