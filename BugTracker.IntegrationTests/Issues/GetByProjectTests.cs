using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Issues
{
    public class GetByProjectTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByProjectTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, IssueType type = IssueType.Task, Priority priority = Priority.Medium)
        {
            var dto = new CreateIssueDto
            {
                Title = title,
                Description = $"Description for {title}",
                Type = type,
                Priority = priority
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
        public async Task GetByProject_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{projectId}/issues");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetByProject_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet.
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGPF");

            await CreateIssueAsync(
                project.Id,
                "Private Issue");

            // User 2 est authentifié mais n'est pas membre.
            await AuthenticateAsync(
                "issues-project-outsider@test.com",
                "issues-project-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/issues");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByProject_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "issues-project-admin@test.com",
                "issues-project-admin-user",
                SystemRole.Admin);

            var unknownProjectId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{unknownProjectId}/issues");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByProject_WhenProjectHasNoIssues_ShouldReturnOkAndEmptyPagedResult()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGPE");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/issues");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<IssueDto>>();

            result.Should().NotBeNull();

            result!.Items.Should().NotBeNull();
            result.Items.Should().BeEmpty();

            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(20);
        }

        [Fact]
        public async Task GetByProject_WhenProjectHasIssues_ShouldReturnOkAndIssues()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGPL");

            var issue1 = await CreateIssueAsync(
                project.Id,
                "Login Bug",
                IssueType.Bug,
                Priority.High);

            var issue2 = await CreateIssueAsync(
                project.Id,
                "Dashboard Feature",
                IssueType.Feature,
                Priority.Medium);

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/issues");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<IssueDto>>();

            result.Should().NotBeNull();

            result!.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);

            result.Items.Should().Contain(i =>
                i.Id == issue1.Id &&
                i.ProjectId == project.Id &&
                i.Title == "Login Bug" &&
                i.Type == IssueType.Bug &&
                i.Priority == Priority.High);

            result.Items.Should().Contain(i =>
                i.Id == issue2.Id &&
                i.ProjectId == project.Id &&
                i.Title == "Dashboard Feature" &&
                i.Type == IssueType.Feature &&
                i.Priority == Priority.Medium);

            result.Items.Should().OnlyContain(
                i => i.ProjectId == project.Id);
        }

        [Fact]
        public async Task GetByProject_ShouldReturnOnlyIssuesOfRequestedProject()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("IGP1");
            var project2 = await CreateProjectAsync("IGP2");

            var project1Issue = await CreateIssueAsync(
                project1.Id,
                "Project 1 Issue");

            await CreateIssueAsync(
                project2.Id,
                "Project 2 Issue");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project1.Id}/issues");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<IssueDto>>();

            result.Should().NotBeNull();

            result!.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);

            result.Items[0].Id.Should().Be(project1Issue.Id);
            result.Items[0].ProjectId.Should().Be(project1.Id);
            result.Items[0].Title.Should().Be("Project 1 Issue");
        }

        [Fact]
        public async Task GetByProject_WhenPaginationIsSpecified_ShouldReturnRequestedPageSizeAndMetadata()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGPP");

            await CreateIssueAsync(
                project.Id,
                "Issue 1");

            await CreateIssueAsync(
                project.Id,
                "Issue 2");

            await CreateIssueAsync(
                project.Id,
                "Issue 3");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/issues?pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<IssueDto>>();

            result.Should().NotBeNull();

            result!.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);

            result.TotalCount.Should().Be(3);
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByProject_WhenSecondPageIsRequested_ShouldReturnRemainingIssues()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("IGP2P");

            await CreateIssueAsync(
                project.Id,
                "Issue A");

            await CreateIssueAsync(
                project.Id,
                "Issue B");

            await CreateIssueAsync(
                project.Id,
                "Issue C");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/issues?pageNumber=2&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<IssueDto>>();

            result.Should().NotBeNull();

            result!.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(2);

            result.TotalCount.Should().Be(3);
            result.Items.Should().HaveCount(1);

            result.Items.Should().OnlyContain(
                i => i.ProjectId == project.Id);
        }
    }
}