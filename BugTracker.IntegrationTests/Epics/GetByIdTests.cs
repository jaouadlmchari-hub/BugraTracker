using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Epics
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

            var epic = await response.Content
                .ReadFromJsonAsync<EpicDto>();

            epic.Should().NotBeNull();

            return epic!;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{epicId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenEpicDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownEpicId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{unknownEpicId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOkAndEpic()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EGI");

            var epic = await CreateEpicAsync(
                project.Id,
                "Authentication Epic");

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{epic.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<EpicDto>();

            result.Should().NotBeNull();

            result!.Id.Should().Be(epic.Id);
            result.ProjectId.Should().Be(project.Id);
            result.Title.Should().Be("Authentication Epic");
            result.Description.Should().Be("Description for Authentication Epic");
            result.ColorCode.Should().Be("#3B82F6");
            result.Status.Should().Be(EpicStatus.Active);
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et l'Epic
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EGF");

            var epic = await CreateEpicAsync(
                project.Id,
                "Private Epic");

            // User 2 devient l'utilisateur courant
            // mais n'est pas membre du projet
            await AuthenticateAsync(
                "epic-outsider@test.com",
                "epic-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{epic.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}