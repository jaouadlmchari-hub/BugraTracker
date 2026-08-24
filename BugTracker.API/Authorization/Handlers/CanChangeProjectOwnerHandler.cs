using BugTracker.API.Authorization.Requirements;
using BugTracker.Application.DTOs.Projects;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BugTracker.API.Authorization.Handlers;

public class CanChangeProjectOwnerHandler
    : AuthorizationHandler<CanChangeProjectOwnerRequirement, ProjectDto>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanChangeProjectOwnerRequirement requirement,
        ProjectDto project)
    {
      
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

       
        var userIdClaim = context.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdClaim, out var currentUserId))
            return Task.CompletedTask;

     
        if (currentUserId == project.OwnerId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}