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
    public class StartSprintTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public StartSprintTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task Start_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprintId}/start",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Start_WhenSprintDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownSprintId = Guid.NewGuid();

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{unknownSprintId}/start",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Start_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SSD");
            var sprint = await CreateSprintAsync(project.Id, "Developer Sprint");

            await AuthenticateAsync(
                "start-developer@test.com",
                "start-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "start-developer@test.com",
                ProjectRole.Developer);

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var status = await GetSprintStatusAsync(sprint.Id);

            status.Should().Be(SprintStatus.Planning);
        }

        [Fact]
        public async Task Start_WhenUserIsManagerAndSprintIsPlanning_ShouldReturnNoContentAndActivateSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SSV");
            var sprint = await CreateSprintAsync(project.Id, "Valid Sprint");

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var status = await GetSprintStatusAsync(sprint.Id);

            status.Should().Be(SprintStatus.Active);
        }

        [Fact]
        public async Task Start_WhenSprintIsAlreadyActive_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SSA");
            var sprint = await CreateSprintAsync(project.Id, "Already Active Sprint");

            var firstStartResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            firstStartResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act
            var secondStartResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            // Assert
            secondStartResponse.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetSprintStatusAsync(sprint.Id);

            status.Should().Be(SprintStatus.Active);
        }

        [Fact]
        public async Task Start_WhenAnotherSprintIsAlreadyActive_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SSO");

            var sprint1 = await CreateSprintAsync(
                project.Id,
                "Sprint 1");

            var sprint2 = await CreateSprintAsync(
                project.Id,
                "Sprint 2");

            var firstStartResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint1.Id}/start",
                null);

            firstStartResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act
            var secondStartResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint2.Id}/start",
                null);

            // Assert
            secondStartResponse.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var sprint1Status = await GetSprintStatusAsync(sprint1.Id);
            var sprint2Status = await GetSprintStatusAsync(sprint2.Id);

            sprint1Status.Should().Be(SprintStatus.Active);
            sprint2Status.Should().Be(SprintStatus.Planning);
        }

        [Fact]
        public async Task Start_WhenSprintIsCompleted_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SSC");
            var sprint = await CreateSprintAsync(project.Id, "Completed Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var completeResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetSprintStatusAsync(sprint.Id);

            status.Should().Be(SprintStatus.Completed);
        }
    }
}