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

            options.AddPolicy(
               "CanViewProject",
               policy =>
               {
                   policy.RequireAuthenticatedUser();

                   policy.AddRequirements(
                        new CanViewProjectRequirement());
               });

            options.AddPolicy(
               "CanManageProject",
               policy =>
               {
                   policy.RequireAuthenticatedUser();
             
                   policy.AddRequirements( 
                        new CanManageProjectRequirement());
               });

            options.AddPolicy(
               "CanChangeProjectOwner",
               policy =>
               {
                   policy.RequireAuthenticatedUser();

                   policy.AddRequirements(
                        new CanChangeProjectOwnerRequirement());
               });



            options.AddPolicy(
               "CanEditIssue",
               policy =>
               {
                   policy.RequireAuthenticatedUser();

                   policy.AddRequirements(
                        new CanEditIssueRequirement());
               });


            options.AddPolicy(
              "CanChangeIssueStatus",
              policy =>
              {
                  policy.RequireAuthenticatedUser();

                  policy.AddRequirements(
                       new CanChangeIssueStatusRequirement());
              });

            options.AddPolicy(
              "CanEditComment",
              policy =>
              {
                  policy.RequireAuthenticatedUser();
            
                  policy.AddRequirements(
                       new CanEditCommentRequirement());
              });


            options.AddPolicy(
             "CanDeleteComment",
             policy =>
             {
                 policy.RequireAuthenticatedUser();

                 policy.AddRequirements(
                      new CanDeleteCommentRequirement());
             });


            options.AddPolicy(
           "CanDeleteAttachment",
           policy =>
           {
               policy.RequireAuthenticatedUser();

               policy.AddRequirements(
                    new CanDeleteAttachmentRequirement());
           });


        });

        services.AddScoped<IAuthorizationHandler, CanViewUserHandler>();
        services.AddScoped<IAuthorizationHandler, CanViewProjectHandler>();
        services.AddScoped<IAuthorizationHandler, CanManageProjectHandler>();
        services.AddScoped<IAuthorizationHandler, CanChangeProjectOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, CanEditIssueHandler>();
        services.AddScoped<IAuthorizationHandler, CanChangeIssueStatusHandler>();
        services.AddScoped<IAuthorizationHandler, CanEditCommentHandler>();
        services.AddScoped<IAuthorizationHandler, CanDeleteCommentHandler>();
        services.AddScoped<IAuthorizationHandler, CanDeleteAttachmentHandler>();


        return services;
    }
}