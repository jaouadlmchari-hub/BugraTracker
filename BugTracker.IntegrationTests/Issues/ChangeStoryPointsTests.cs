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
    public class ChangeStoryPointsTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangeStoryPointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        // =========================================================
        // Helpers
        // =========================================================

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

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, int? storyPoints = null, Guid? assigneeId = null)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Description = $"Description for {title}",
                Type = IssueType.Task,
                Priority = Priority.Medium,
                StoryPoints = storyPoints,
                AssigneeId = assigneeId
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/issues",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var issue = await response.Content.ReadFromJsonAsync<IssueDto>();

            issue.Should().NotBeNull();

            return issue!;
        }

        private async Task<Guid> AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                userId = user.Id;

                var membership = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                };

                dbContext.Set<ProjectMember>().Add(membership);

                await dbContext.SaveChangesAsync();
            });

            return userId;
        }

        private async Task<int?> GetStoryPointsAsync(Guid issueId)
        {
            int? storyPoints = null;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var issue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issueId);

                storyPoints = issue.StoryPoints;
            });

            return storyPoints;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task ChangeStoryPoints_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = 5
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issueId}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangeStoryPoints_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownIssueId = Guid.NewGuid();

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = 8
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{unknownIssueId}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ChangeStoryPoints_WhenDeveloperHasNoEditRights_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSPF");

            var issue = await CreateIssueAsync(
                project.Id,
                "Protected Issue",
                3);

            await AuthenticateAsync(
                "storypoints-developer@test.com",
                "storypoints-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "storypoints-developer@test.com",
                ProjectRole.Developer);

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = 8
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var storyPoints = await GetStoryPointsAsync(issue.Id);

            storyPoints.Should().Be(3);
        }

        [Fact]
        public async Task ChangeStoryPoints_WhenReporterChangesStoryPoints_ShouldReturnNoContentAndPersistValue()
        {
            // Arrange

            // Manager crée le projet.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSPR");

            // Developer devient membre.
            await AuthenticateAsync(
                "storypoints-reporter@test.com",
                "storypoints-reporter-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "storypoints-reporter@test.com",
                ProjectRole.Developer);

            // Le Developer crée l'Issue :
            // il devient Reporter.
            var issue = await CreateIssueAsync(
                project.Id,
                "Reporter Issue",
                3);

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = 8
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var storyPoints = await GetStoryPointsAsync(issue.Id);

            storyPoints.Should().Be(8);
        }

        [Fact]
        public async Task ChangeStoryPoints_WhenAssigneeChangesStoryPoints_ShouldReturnNoContentAndPersistValue()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSPA");

            await AuthenticateAsync(
                "storypoints-assignee@test.com",
                "storypoints-assignee-user");

            var assigneeId = await AddExistingUserAsMemberAsync(
                project.Id,
                "storypoints-assignee@test.com",
                ProjectRole.Developer);

            // Revenir au Manager pour créer l'Issue avec cet Assignee.
            await AuthenticateAsync(
                "authenticated2@test.com",
                "authenticated2-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "authenticated2@test.com",
                ProjectRole.Manager);

            var issue = await CreateIssueAsync(
                project.Id,
                "Assigned Issue",
                5,
                assigneeId);

            // L'Assignee devient l'utilisateur courant.
            await AuthenticateAsync(
                "storypoints-assignee2@test.com",
                "storypoints-assignee2-user");

            var secondAssigneeId = await AddExistingUserAsMemberAsync(
                project.Id,
                "storypoints-assignee2@test.com",
                ProjectRole.Developer);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedIssue = await dbContext.Set<Issue>()
                    .SingleAsync(i => i.Id == issue.Id);

                savedIssue.AssigneeId = secondAssigneeId;

                await dbContext.SaveChangesAsync();
            });

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = 13
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var storyPoints = await GetStoryPointsAsync(issue.Id);

            storyPoints.Should().Be(13);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task ChangeStoryPoints_WhenValueIsOutsideAllowedRange_ShouldReturnBadRequest(int invalidStoryPoints)
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSPI");

            var issue = await CreateIssueAsync(
                project.Id,
                "Invalid Story Points Issue",
                5);

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = invalidStoryPoints
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var storyPoints = await GetStoryPointsAsync(issue.Id);

            storyPoints.Should().Be(5);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public async Task ChangeStoryPoints_WhenValueIsInsideAllowedRange_ShouldReturnNoContent(int storyPoints)
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync(
                $"CSP{storyPoints}");


            var issue = await CreateIssueAsync(
                project.Id,
                "Boundary Story Points Issue",
                5);

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = storyPoints
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var savedStoryPoints = await GetStoryPointsAsync(issue.Id);

            savedStoryPoints.Should().Be(storyPoints);
        }

        [Fact]
        public async Task ChangeStoryPoints_WhenValueIsNull_ShouldReturnNoContentAndClearStoryPoints()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSPN");

            var issue = await CreateIssueAsync(
                project.Id,
                "Clear Story Points Issue",
                8);

            var dto = new ChangeStoryPointsDto
            {
                StoryPoints = null
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/story-points",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var storyPoints = await GetStoryPointsAsync(issue.Id);

            storyPoints.Should().BeNull();
        }
    }
}