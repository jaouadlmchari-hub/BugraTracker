using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Issues
{
    public class AssignIssueTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public AssignIssueTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<ProjectDto> CreateProjectAsync(string key)
        {
            var dto = new CreateProjectDto
            {
                Name = $"Project {key}",
                Key = key,
                Description = $"Integration test project {key}"
            };

            var response = await Client.PostAsJsonAsync("/api/projects", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var project = await response.Content.ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();

            return project!;
        }

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Description = $"Description for {title}",
                Type = IssueType.Task,
                Priority = Priority.Medium
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/issues",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var issue = await response.Content.ReadFromJsonAsync<IssueDto>();

            issue.Should().NotBeNull();

            return issue!;
        }

        private async Task<Guid> CreateUserAndMemberAsync(Guid projectId, string email, string username, ProjectRole role)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(user);
                await dbContext.SaveChangesAsync();

                userId = user.Id;

                dbContext.Set<ProjectMember>().Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                });

                await dbContext.SaveChangesAsync();
            });

            return userId;
        }

        private async Task AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                dbContext.Set<ProjectMember>().Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                });

                await dbContext.SaveChangesAsync();
            });
        }

        private async Task<Guid?> GetAssigneeIdAsync(Guid issueId)
        {
            Guid? assigneeId = null;

            await ExecuteDbContextAsync(async dbContext =>
            {
                assigneeId = await dbContext.Set<Issue>()
                    .Where(i => i.Id == issueId)
                    .Select(i => i.AssigneeId)
                    .SingleAsync();
            });

            return assigneeId;
        }

        [Fact]
        public async Task Assign_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new AssignIssueDto
            {
                UserId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/assignee",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Assign_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new AssignIssueDto
            {
                UserId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/assignee",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Assign_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IAF");
            var issue = await CreateIssueAsync(project.Id, "Protected Issue");

            var targetUserId = await CreateUserAndMemberAsync(
                project.Id,
                "assign-target@test.com",
                "assign-target",
                ProjectRole.Developer);

            await AuthenticateAsync(
                "assign-developer@test.com",
                "assign-developer");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "assign-developer@test.com",
                ProjectRole.Developer);

            var dto = new AssignIssueDto
            {
                UserId = targetUserId
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/assignee",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var assigneeId = await GetAssigneeIdAsync(issue.Id);

            assigneeId.Should().BeNull();
        }

        [Fact]
        public async Task Assign_WhenManagerAssignsProjectMember_ShouldReturnNoContentAndAssignIssue()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IAV");
            var issue = await CreateIssueAsync(project.Id, "Assignable Issue");

            var targetUserId = await CreateUserAndMemberAsync(
                project.Id,
                "assigned-user@test.com",
                "assigned-user",
                ProjectRole.Developer);

            var dto = new AssignIssueDto
            {
                UserId = targetUserId
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/assignee",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var assigneeId = await GetAssigneeIdAsync(issue.Id);

            assigneeId.Should().Be(targetUserId);
        }
    }
}