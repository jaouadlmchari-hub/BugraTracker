using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BugTracker.API.Authorization.Handlers;

public class CanViewUserHandler : AuthorizationHandler<CanViewUserRequirement, Guid>
{
    private readonly IProjectMemberService _projectMemberService;

    public CanViewUserHandler(IProjectMemberService projectMemberService)
    {
        _projectMemberService = projectMemberService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanViewUserRequirement requirement,
        Guid targetUserId)
    {
        // 1. Admin → autorisé
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Récupérer l'id de l'utilisateur connecté depuis le JWT
        var currentUserIdClaim = context.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return;
        }

        // 3. L'utilisateur consulte son propre profil
        if (currentUserId == targetUserId)
        {
            context.Succeed(requirement);
            return;
        }

        // 4. Vérifier s'ils partagent au moins un projet
        var shareProject =
            await _projectMemberService.ShareAnyProjectAsync(
                currentUserId,
                targetUserId);

        if (shareProject)
        {
            context.Succeed(requirement);
        }
    }
}