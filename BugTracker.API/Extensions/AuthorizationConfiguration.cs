using BugTracker.API.Authorization.Handlers;
using BugTracker.API.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace BugTracker.API.Extensions;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "CanViewUser",
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new CanViewUserRequirement());
                });
        });

        services.AddScoped<IAuthorizationHandler, CanViewUserHandler>();

        return services;
    }
}