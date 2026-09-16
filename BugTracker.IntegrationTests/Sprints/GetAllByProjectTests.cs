using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.DTOs.Sprints;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Sprints
{
    public class GetAllByProjectTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetAllByProjectTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<SprintDto> CreateSprintAsync(Guid projectId, string name)
        {
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(14);

            var dto = new CreateSprintDto
            {
                Name = name,
                Goal = $"Goal for {name}",
                StartDate = startDate,
                EndDate = endDate
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/sprints",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var sprint = await response.Content.ReadFromJsonAsync<SprintDto>();

            sprint.Should().NotBeNull();

            return sprint!;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetAllByProject_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{projectId}/sprints");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllByProject_WhenProjectHasNoSprints_ShouldReturnOkAndEmptyList()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GSE");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/sprints");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var sprints = await response.Content
                .ReadFromJsonAsync<IEnumerable<SprintDto>>();

            sprints.Should().NotBeNull();
            sprints.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllByProject_WhenUserIsProjectMember_ShouldReturnOkAndProjectSprints()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GSL");

            var sprint1 = await CreateSprintAsync(
                project.Id,
                "Sprint 1");

            var sprint2 = await CreateSprintAsync(
                project.Id,
                "Sprint 2");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/sprints");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var sprints = await response.Content
                .ReadFromJsonAsync<IEnumerable<SprintDto>>();

            sprints.Should().NotBeNull();

            var sprintList = sprints!.ToList();

            sprintList.Should().HaveCount(2);

            sprintList.Should().Contain(s =>
                s.Id == sprint1.Id &&
                s.ProjectId == project.Id &&
                s.Name == "Sprint 1" &&
                s.Status == SprintStatus.Planning);

            sprintList.Should().Contain(s =>
                s.Id == sprint2.Id &&
                s.ProjectId == project.Id &&
                s.Name == "Sprint 2" &&
                s.Status == SprintStatus.Planning);
        }

        [Fact]
        public async Task GetAllByProject_ShouldReturnOnlySprintsOfRequestedProject()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("GSP1");
            var project2 = await CreateProjectAsync("GSP2");

            var project1Sprint = await CreateSprintAsync(
                project1.Id,
                "Project 1 Sprint");

            await CreateSprintAsync(
                project2.Id,
                "Project 2 Sprint");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project1.Id}/sprints");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var sprints = await response.Content
                .ReadFromJsonAsync<IEnumerable<SprintDto>>();

            sprints.Should().NotBeNull();

            var sprintList = sprints!.ToList();

            sprintList.Should().HaveCount(1);

            sprintList[0].Id.Should().Be(project1Sprint.Id);
            sprintList[0].ProjectId.Should().Be(project1.Id);
            sprintList[0].Name.Should().Be("Project 1 Sprint");
        }

        [Fact]
        public async Task GetAllByProject_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et un sprint
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GSF");

            await CreateSprintAsync(
                project.Id,
                "Private Sprint");

            // User 2 devient utilisateur courant
            // mais n'est pas membre du projet
            await AuthenticateAsync(
                "sprints-outsider@test.com",
                "sprints-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/sprints");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}