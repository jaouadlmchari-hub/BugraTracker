using BugTracker.Application.DTOs.ActivityLogs;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.ActivityLogs
{
    public class GetByIssueTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIssueTests(CustomWebApplicationFactory factory) : base(factory)
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
                Key = key
            };

            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var project = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

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

            var issue = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            issue.Should().NotBeNull();

            return issue!;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetByIssue_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issueId}/activity-logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetByIssue_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownIssueId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{unknownIssueId}/activity-logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByIssue_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et l'Issue.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ALF");

            var issue = await CreateIssueAsync(
                project.Id,
                "Private Activity Issue");

            // User 2 est authentifié mais n'est pas membre.
            await AuthenticateAsync(
                "activity-outsider@test.com",
                "activity-outsider");

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/activity-logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByIssue_WhenIssueWasCreated_ShouldReturnCreatedActivityLog()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ALC");

            var issue = await CreateIssueAsync(
                project.Id,
                "Created Activity Issue");

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/activity-logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var logs = await response.Content
                .ReadFromJsonAsync<IEnumerable<ActivityLogDto>>();

            logs.Should().NotBeNull();

            var list = logs!.ToList();

            list.Should().ContainSingle();
        }

        [Fact]
        public async Task GetByIssue_WhenIssueStatusChanges_ShouldReturnBothActivityLogs()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ALS");

            var issue = await CreateIssueAsync(
                project.Id,
                "Status Activity Issue");

            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            var changeStatusResponse = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue.Id}/status",
                dto);

            changeStatusResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}/activity-logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var logs = await response.Content
                .ReadFromJsonAsync<IEnumerable<ActivityLogDto>>();

            logs.Should().NotBeNull();

            var list = logs!.ToList();

            // 1 → Issue Created
            // 2 → Status Todo → InProgress
            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIssue_ShouldReturnOnlyLogsOfRequestedIssue()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ALI");

            var issue1 = await CreateIssueAsync(
                project.Id,
                "Issue 1");

            var issue2 = await CreateIssueAsync(
                project.Id,
                "Issue 2");

            // Issue 1 reçoit une activité supplémentaire.
            var dto = new ChangeIssueStatusDto
            {
                NewStatus = IssueStatus.InProgress
            };

            var changeStatusResponse = await Client.PatchAsJsonAsync(
                $"/api/issues/{issue1.Id}/status",
                dto);

            changeStatusResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issue1.Id}/activity-logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var logs = await response.Content
                .ReadFromJsonAsync<IEnumerable<ActivityLogDto>>();

            logs.Should().NotBeNull();

            // issue1 :
            // Created + StatusChanged = 2
            //
            // Le Created de issue2 ne doit pas apparaître.
            logs!.Should().HaveCount(2);
        }
    }
}