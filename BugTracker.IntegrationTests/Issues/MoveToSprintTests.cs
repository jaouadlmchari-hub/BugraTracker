using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.DTOs.Sprints;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Issues
{
    public class MoveToSprintTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public MoveToSprintTests(CustomWebApplicationFactory factory) : base(factory)
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

            return (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
        }

        private async Task<SprintDto> CreateSprintAsync(Guid projectId, string name)
        {
            var dto = new CreateSprintDto
            {
                Name = name,
                Goal = $"Goal for {name}",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/sprints",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<SprintDto>())!;
        }

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, Guid? sprintId = null)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Type = IssueType.Task,
                Priority = Priority.Medium,
                SprintId = sprintId
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/issues",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<IssueDto>())!;
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

        private async Task<Guid?> GetSprintIdAsync(Guid issueId)
        {
            Guid? sprintId = null;

            await ExecuteDbContextAsync(async dbContext =>
            {
                sprintId = await dbContext.Set<Issue>()
                    .Where(i => i.Id == issueId)
                    .Select(i => i.SprintId)
                    .SingleAsync();
            });

            return sprintId;
        }

        [Fact]
        public async Task MoveToSprint_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new MoveIssueToSprintDto
            {
                SprintId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/sprint",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task MoveToSprint_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new MoveIssueToSprintDto
            {
                SprintId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/sprint",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task MoveToSprint_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IMSF");
            var sprint = await CreateSprintAsync(project.Id, "Sprint 1");
            var issue = await CreateIssueAsync(project.Id, "Issue");

            await AuthenticateAsync(
                "move-sprint-developer@test.com",
                "move-sprint-developer");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "move-sprint-developer@test.com",
                ProjectRole.Developer);

            var dto = new MoveIssueToSprintDto
            {
                SprintId = sprint.Id
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/sprint",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var sprintId = await GetSprintIdAsync(issue.Id);

            sprintId.Should().BeNull();
        }

        [Fact]
        public async Task MoveToSprint_WhenManagerMovesIssueToSprint_ShouldReturnNoContentAndPersistSprint()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IMSV");
            var sprint = await CreateSprintAsync(project.Id, "Sprint Target");
            var issue = await CreateIssueAsync(project.Id, "Issue To Move");

            var dto = new MoveIssueToSprintDto
            {
                SprintId = sprint.Id
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/sprint",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var sprintId = await GetSprintIdAsync(issue.Id);

            sprintId.Should().Be(sprint.Id);
        }

        [Fact]
        public async Task MoveToSprint_WhenSprintIdIsNull_ShouldReturnNoContentAndMoveIssueToBacklog()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IMSN");
            var sprint = await CreateSprintAsync(project.Id, "Original Sprint");

            var issue = await CreateIssueAsync(
                project.Id,
                "Backlog Issue",
                sprint.Id);

            var dto = new MoveIssueToSprintDto
            {
                SprintId = null
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/sprint",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var sprintId = await GetSprintIdAsync(issue.Id);

            sprintId.Should().BeNull();
        }
    }
}