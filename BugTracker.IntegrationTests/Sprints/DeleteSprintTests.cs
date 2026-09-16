using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.DTOs.Sprints;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Sprints
{
    public class DeleteSprintTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeleteSprintTests(CustomWebApplicationFactory factory) : base(factory)
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
            var dto = new CreateSprintDto
            {
                Name = name,
                Goal = $"Goal for {name}",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/sprints",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var sprint = await response.Content.ReadFromJsonAsync<SprintDto>();

            sprint.Should().NotBeNull();

            return sprint!;
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

        private async Task<bool> SprintExistsAsync(Guid sprintId)
        {
            var exists = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                exists = await dbContext.Set<Sprint>()
                    .AnyAsync(s => s.Id == sprintId);
            });

            return exists;
        }

        private async Task<SprintStatus> GetSprintStatusAsync(Guid sprintId)
        {
            SprintStatus status = default;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var sprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprintId);

                status = sprint.Status;
            });

            return status;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Delete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/sprints/{sprintId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_WhenSprintDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownSprintId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/sprints/{unknownSprintId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("DSD");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Developer Delete Sprint");

            await AuthenticateAsync(
                "delete-developer@test.com",
                "delete-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "delete-developer@test.com",
                ProjectRole.Developer);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/sprints/{sprint.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var exists = await SprintExistsAsync(sprint.Id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WhenSprintIsActive_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("DSA");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Active Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var statusBeforeDelete = await GetSprintStatusAsync(sprint.Id);

            statusBeforeDelete.Should().Be(SprintStatus.Active);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/sprints/{sprint.Id}");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var exists = await SprintExistsAsync(sprint.Id);

            exists.Should().BeTrue();

            var statusAfterDelete = await GetSprintStatusAsync(sprint.Id);

            statusAfterDelete.Should().Be(SprintStatus.Active);
        }

        [Fact]
        public async Task Delete_WhenUserIsManagerAndSprintIsPlanning_ShouldReturnNoContentAndDeleteSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("DSP");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Planning Sprint");

            var existsBeforeDelete = await SprintExistsAsync(sprint.Id);

            existsBeforeDelete.Should().BeTrue();

            // Act
            var response = await Client.DeleteAsync(
                $"/api/sprints/{sprint.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var existsAfterDelete = await SprintExistsAsync(sprint.Id);

            existsAfterDelete.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_WhenUserIsManagerAndSprintIsCompleted_ShouldReturnNoContentAndDeleteSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("DSC");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Completed Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var completeResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var statusBeforeDelete = await GetSprintStatusAsync(sprint.Id);

            statusBeforeDelete.Should().Be(SprintStatus.Completed);

            // Act
            var response = await Client.DeleteAsync(
                $"/api/sprints/{sprint.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var existsAfterDelete = await SprintExistsAsync(sprint.Id);

            existsAfterDelete.Should().BeFalse();
        }
    }
}