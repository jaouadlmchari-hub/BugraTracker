using BugTracker.Application.DTOs.Epics;
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
    public class MoveToEpicTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public MoveToEpicTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<EpicDto> CreateEpicAsync(Guid projectId, string title)
        {
            var dto = new CreateEpicDto
            {
                Title = title,
                Description = $"Description for {title}",
                ColorCode = "#3B82F6"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/epics",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<EpicDto>())!;
        }

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, Guid? epicId = null)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Type = IssueType.Task,
                Priority = Priority.Medium,
                EpicId = epicId
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

        private async Task<Guid?> GetEpicIdAsync(Guid issueId)
        {
            Guid? epicId = null;

            await ExecuteDbContextAsync(async dbContext =>
            {
                epicId = await dbContext.Set<Issue>()
                    .Where(i => i.Id == issueId)
                    .Select(i => i.EpicId)
                    .SingleAsync();
            });

            return epicId;
        }

        [Fact]
        public async Task MoveToEpic_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new MoveIssueToEpicDto
            {
                EpicId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/epic",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task MoveToEpic_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new MoveIssueToEpicDto
            {
                EpicId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{Guid.NewGuid()}/epic",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task MoveToEpic_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IMEF");
            var epic = await CreateEpicAsync(project.Id, "Epic");
            var issue = await CreateIssueAsync(project.Id, "Issue");

            await AuthenticateAsync(
                "move-epic-developer@test.com",
                "move-epic-developer");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "move-epic-developer@test.com",
                ProjectRole.Developer);

            var dto = new MoveIssueToEpicDto
            {
                EpicId = epic.Id
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/epic",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var epicId = await GetEpicIdAsync(issue.Id);

            epicId.Should().BeNull();
        }

        [Fact]
        public async Task MoveToEpic_WhenManagerMovesIssueToEpic_ShouldReturnNoContentAndPersistEpic()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IMEV");
            var epic = await CreateEpicAsync(project.Id, "Target Epic");
            var issue = await CreateIssueAsync(project.Id, "Issue To Move");

            var dto = new MoveIssueToEpicDto
            {
                EpicId = epic.Id
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/epic",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var epicId = await GetEpicIdAsync(issue.Id);

            epicId.Should().Be(epic.Id);
        }

        [Fact]
        public async Task MoveToEpic_WhenEpicIdIsNull_ShouldReturnNoContentAndRemoveIssueFromEpic()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IMEN");
            var epic = await CreateEpicAsync(project.Id, "Original Epic");

            var issue = await CreateIssueAsync(
                project.Id,
                "Issue With Epic",
                epic.Id);

            var dto = new MoveIssueToEpicDto
            {
                EpicId = null
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/epic",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var epicId = await GetEpicIdAsync(issue.Id);

            epicId.Should().BeNull();
        }
    }
}