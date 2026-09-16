using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Epics
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

            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var project = await response.Content.ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();

            return project!;
        }

        private async Task<EpicDto> CreateEpicAsync(Guid projectId, string title, string colorCode = "#3B82F6")
        {
            var dto = new CreateEpicDto
            {
                Title = title,
                Description = $"Description for {title}",
                ColorCode = colorCode
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
        public async Task GetAllByProject_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{projectId}/epics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllByProject_WhenProjectHasNoEpics_ShouldReturnOkAndEmptyList()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EGE");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/epics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var epics = await response.Content
                .ReadFromJsonAsync<IEnumerable<EpicDto>>();

            epics.Should().NotBeNull();
            epics.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllByProject_WhenUserIsProjectMember_ShouldReturnOkAndProjectEpics()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EGL");

            var epic1 = await CreateEpicAsync(
                project.Id,
                "Authentication Epic",
                "#3B82F6");

            var epic2 = await CreateEpicAsync(
                project.Id,
                "Reporting Epic",
                "#22C55E");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/epics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var epics = await response.Content
                .ReadFromJsonAsync<IEnumerable<EpicDto>>();

            epics.Should().NotBeNull();

            var epicList = epics!.ToList();

            epicList.Should().HaveCount(2);

            epicList.Should().Contain(e =>
                e.Id == epic1.Id &&
                e.ProjectId == project.Id &&
                e.Title == "Authentication Epic" &&
                e.ColorCode == "#3B82F6" &&
                e.Status == EpicStatus.Active);

            epicList.Should().Contain(e =>
                e.Id == epic2.Id &&
                e.ProjectId == project.Id &&
                e.Title == "Reporting Epic" &&
                e.ColorCode == "#22C55E" &&
                e.Status == EpicStatus.Active);
        }

        [Fact]
        public async Task GetAllByProject_ShouldReturnOnlyEpicsOfRequestedProject()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("EGP1");
            var project2 = await CreateProjectAsync("EGP2");

            var project1Epic = await CreateEpicAsync(
                project1.Id,
                "Project 1 Epic");

            await CreateEpicAsync(
                project2.Id,
                "Project 2 Epic");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project1.Id}/epics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var epics = await response.Content
                .ReadFromJsonAsync<IEnumerable<EpicDto>>();

            epics.Should().NotBeNull();

            var epicList = epics!.ToList();

            epicList.Should().HaveCount(1);

            epicList[0].Id.Should().Be(project1Epic.Id);
            epicList[0].ProjectId.Should().Be(project1.Id);
            epicList[0].Title.Should().Be("Project 1 Epic");
        }

        [Fact]
        public async Task GetAllByProject_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et un Epic
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EGF");

            await CreateEpicAsync(
                project.Id,
                "Private Epic");

            // User 2 devient utilisateur courant
            // mais n'est pas membre du projet
            await AuthenticateAsync(
                "epics-outsider@test.com",
                "epics-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/epics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}