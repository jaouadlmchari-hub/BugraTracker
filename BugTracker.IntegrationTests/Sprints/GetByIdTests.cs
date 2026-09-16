using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.DTOs.Sprints;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Sprints
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
                Goal = "Integration test sprint",
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
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/sprints/{sprintId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenSprintDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownSprintId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/sprints/{unknownSprintId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOkAndSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GBI");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Sprint GetById");

            // Act
            var response = await Client.GetAsync(
                $"/api/sprints/{sprint.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<SprintDto>();

            result.Should().NotBeNull();

            result!.Id.Should().Be(sprint.Id);
            result.ProjectId.Should().Be(project.Id);
            result.Name.Should().Be("Sprint GetById");
            result.Goal.Should().Be("Integration test sprint");
            result.Status.Should().Be(SprintStatus.Planning);
            result.StartDate.Should().Be(sprint.StartDate);
            result.EndDate.Should().Be(sprint.EndDate);
            result.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et le sprint
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GBF");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Private Sprint");

            // User 2 devient l'utilisateur courant
            // mais n'est pas membre du projet
            await AuthenticateAsync(
                "sprint-outsider@test.com",
                "sprint-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/sprints/{sprint.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}