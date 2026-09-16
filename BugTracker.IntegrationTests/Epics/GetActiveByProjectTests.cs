using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Epics
{
    public class GetActiveByProjectTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetActiveByProjectTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task SetEpicStatusAsync(Guid epicId, EpicStatus status)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var epic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epicId);

                epic.Status = status;

                await dbContext.SaveChangesAsync();
            });
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetActiveByProject_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{projectId}/epics/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetActiveByProject_WhenProjectHasNoEpics_ShouldReturnOkAndEmptyList()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EAE");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/epics/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var epics = await response.Content
                .ReadFromJsonAsync<IEnumerable<EpicDto>>();

            epics.Should().NotBeNull();
            epics.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActiveByProject_WhenProjectHasActiveEpics_ShouldReturnOnlyActiveEpics()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EAL");

            var activeEpic1 = await CreateEpicAsync(
                project.Id,
                "Active Epic 1");

            var activeEpic2 = await CreateEpicAsync(
                project.Id,
                "Active Epic 2");

            var completedEpic = await CreateEpicAsync(
                project.Id,
                "Completed Epic");

            var archivedEpic = await CreateEpicAsync(
                project.Id,
                "Archived Epic");

            await SetEpicStatusAsync(
                completedEpic.Id,
                EpicStatus.Completed);

            await SetEpicStatusAsync(
                archivedEpic.Id,
                EpicStatus.Archived);

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/epics/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var epics = await response.Content
                .ReadFromJsonAsync<IEnumerable<EpicDto>>();

            epics.Should().NotBeNull();

            var epicList = epics!.ToList();

            epicList.Should().HaveCount(2);

            epicList.Should().Contain(e =>
                e.Id == activeEpic1.Id &&
                e.Status == EpicStatus.Active);

            epicList.Should().Contain(e =>
                e.Id == activeEpic2.Id &&
                e.Status == EpicStatus.Active);

            epicList.Should().NotContain(e =>
                e.Id == completedEpic.Id);

            epicList.Should().NotContain(e =>
                e.Id == archivedEpic.Id);

            epicList.Should().OnlyContain(e =>
                e.Status == EpicStatus.Active);
        }

        [Fact]
        public async Task GetActiveByProject_ShouldReturnOnlyActiveEpicsOfRequestedProject()
        {
            // Arrange
            await AuthenticateAsync();

            var project1 = await CreateProjectAsync("EAP1");
            var project2 = await CreateProjectAsync("EAP2");

            var project1Epic = await CreateEpicAsync(
                project1.Id,
                "Project 1 Active Epic");

            await CreateEpicAsync(
                project2.Id,
                "Project 2 Active Epic");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project1.Id}/epics/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var epics = await response.Content
                .ReadFromJsonAsync<IEnumerable<EpicDto>>();

            epics.Should().NotBeNull();

            var epicList = epics!.ToList();

            epicList.Should().HaveCount(1);

            epicList[0].Id.Should().Be(project1Epic.Id);
            epicList[0].ProjectId.Should().Be(project1.Id);
            epicList[0].Status.Should().Be(EpicStatus.Active);
        }

        [Fact]
        public async Task GetActiveByProject_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet et l'Epic
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EAF");

            await CreateEpicAsync(
                project.Id,
                "Private Active Epic");

            // User 2 devient utilisateur courant
            // mais n'est pas membre du projet
            await AuthenticateAsync(
                "active-epics-outsider@test.com",
                "active-epics-outsider-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/epics/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}