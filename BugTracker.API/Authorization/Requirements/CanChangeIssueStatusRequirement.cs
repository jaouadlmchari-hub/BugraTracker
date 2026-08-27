using Microsoft.AspNetCore.Authorization;

namespace BugTracker.API.Authorization.Requirements
{
    public class CanChangeIssueStatusRequirement : IAuthorizationRequirement
    {
    }
}
