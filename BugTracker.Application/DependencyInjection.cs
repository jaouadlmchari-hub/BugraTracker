using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Services;
using BugTracker.Application.Validators.Sprints;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BugTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectMemberService, ProjectMemberService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        services.AddValidatorsFromAssemblyContaining<CreateSprintValidator>();

        return services;
    }
}