using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Issues
{
    public class GetByIdTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIdTests(CustomWebApplicationFactory factory) : base(factory)
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
                Type = IssueType.Bug,
                Priority = Priority.High,
                StoryPoints = 5,
                DueDate = DateTime.UtcNow.Date.AddDays(7)
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
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issueId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenIssueDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownIssueId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{unknownIssueId}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOkAndIssue()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGI");

            var issue = await CreateIssueAsync(
                project.Id,
                "Authentication Bug");

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<IssueDto>();

            result.Should().NotBeNull();

            result!.Id.Should().Be(issue.Id);
            result.ProjectId.Should().Be(project.Id);

            result.Title.Should().Be("Authentication Bug");
            result.Description.Should().Be("Description for Authentication Bug");

            result.Type.Should().Be(IssueType.Bug);
            result.Status.Should().Be(IssueStatus.Todo);
            result.Priority.Should().Be(Priority.High);

            result.StoryPoints.Should().Be(5);
            result.DueDate.Should().Be(issue.DueDate);

            result.ReporterId.Should().Be(issue.ReporterId);

            result.EpicId.Should().BeNull();
            result.SprintId.Should().BeNull();
            result.AssigneeId.Should().BeNull();

            result.DisplayOrder.Should().Be(issue.DisplayOrder);
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et l'Issue.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGF");

            var issue = await CreateIssueAsync(
                project.Id,
                "Private Issue");

            // User 2 est authentifié mais n'est pas membre du projet.
            await AuthenticateAsync(
                "issue-get-outsider@test.com",
                "issue-get-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/issues/{issue.Id}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.Forbidden);
        }
    }
}