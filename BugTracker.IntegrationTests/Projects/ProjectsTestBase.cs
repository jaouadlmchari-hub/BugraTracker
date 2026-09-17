using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BugTracker.IntegrationTests.Projects
{
    public abstract class ProjectsTestBase : IntegrationTestBase
    {
        protected ProjectsTestBase(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected async Task<ProjectDto> CreateProjectAsync(string key, string? name = null)
        {
            var dto = new CreateProjectDto
            {
                Name = name ?? $"Project {key}",
                Key = key,
                Description = $"Description {key}"
            };

            var response = await Client.PostAsJsonAsync("/api/projects", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
        }

        protected async Task<UserDto> CreateUserAsync(string email, string username)
        {
            var dto = new CreateUserDto
            {
                Email = email,
                Username = username,
                FullName = $"Full Name {username}",
                Password = "Password123!"
            };

            var response = await Client.PostAsJsonAsync("/api/users", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<UserDto>())!;
        }

        protected async Task AddProjectMemberAsync(Guid projectId, Guid userId, ProjectRole role)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                dbContext.Set<ProjectMember>().Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Role = role
                });

                await dbContext.SaveChangesAsync();
            });
        }

        protected async Task<bool> ProjectExistsAsync(Guid projectId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext.Set<Project>()
                    .AnyAsync(p => p.Id == projectId);
            });

            return exists;
        }
    }
}