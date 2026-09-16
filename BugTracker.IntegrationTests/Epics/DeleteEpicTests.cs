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
    public class DeleteEpicTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeleteEpicTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                var membership = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                };

                dbContext.Set<ProjectMember>().Add(membership);

                await dbContext.SaveChangesAsync();
            });
        }

        private async Task<bool> EpicExistsAsync(Guid epicId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext.Set<Epic>()
                    .AnyAsync(e => e.Id == epicId);
            });

            return exists;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Delete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/epics/{epicId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_WhenEpicDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownEpicId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/epics/{unknownEpicId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenUserIsDeveloper_ShouldReturnForbiddenAndKeepEpic()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EDD");

            var epic = await CreateEpicAsync(
                project.Id,
                "Developer Delete Epic");

            await AuthenticateAsync(
                "delete-epic-developer@test.com",
                "delete-epic-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "delete-epic-developer@test.com",
                ProjectRole.Developer);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/epics/{epic.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var exists = await EpicExistsAsync(epic.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WhenUserIsManager_ShouldReturnNoContentAndDeleteEpic()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EDV");

            var epic = await CreateEpicAsync(
                project.Id,
                "Epic To Delete");

            var existsBeforeDelete = await EpicExistsAsync(epic.Id);

            existsBeforeDelete.Should().BeTrue();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/epics/{epic.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var existsAfterDelete = await EpicExistsAsync(epic.Id);

            existsAfterDelete.Should().BeFalse();
        }
    }
}