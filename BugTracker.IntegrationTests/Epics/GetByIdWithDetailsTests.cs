using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Epics
{
    public class GetByIdWithDetailsTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIdWithDetailsTests(CustomWebApplicationFactory factory) : base(factory)
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

            var epic = await response.Content.ReadFromJsonAsync<EpicDto>();

            epic.Should().NotBeNull();

            return epic!;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetByIdWithDetails_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{epicId}/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetByIdWithDetails_WhenEpicDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownEpicId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{unknownEpicId}/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByIdWithDetails_WhenUserIsProjectMember_ShouldReturnOkAndEpicDetails()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EGD");

            var epic = await CreateEpicAsync(
                project.Id,
                "Detailed Epic");

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{epic.Id}/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<EpicDetailsDto>();

            result.Should().NotBeNull();

            result!.Id.Should().Be(epic.Id);
            result.ProjectId.Should().Be(project.Id);
            result.Title.Should().Be("Detailed Epic");
            result.Description.Should().Be("Description for Detailed Epic");
            result.ColorCode.Should().Be("#3B82F6");
            result.Status.Should().Be(EpicStatus.Active);

            result.Issues.Should().NotBeNull();
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdWithDetails_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et l'Epic
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EDF");

            var epic = await CreateEpicAsync(
                project.Id,
                "Private Detailed Epic");

            // User 2 devient utilisateur courant
            // mais n'est pas membre du projet
            await AuthenticateAsync(
                "epic-details-outsider@test.com",
                "epic-details-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/epics/{epic.Id}/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}