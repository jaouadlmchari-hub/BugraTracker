using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BugTracker.API.Authorization.Handlers;

public class CanViewProjectHandler
    : AuthorizationHandler<CanViewProjectRequirement, Guid>
{
    private readonly IProjectMemberService _projectMemberService;

    public CanViewProjectHandler(
        IProjectMemberService projectMemberService)
    {
        _projectMemberService = projectMemberService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanViewProjectRequirement requirement,
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

        // Vérifier si l'utilisateur appartient au projet
        var isMember =
            await _projectMemberService.IsMemberAsync(
                projectId,
                currentUserId);

        if (isMember)
            context.Succeed(requirement);
    }
}