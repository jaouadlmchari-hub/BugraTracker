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
    public class CompleteSprintTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CompleteSprintTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<(SprintStatus Status, DateTime? CompletedAt)> GetSprintStateAsync(Guid sprintId)
        {
            var status = default(SprintStatus);
            DateTime? completedAt = null;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var sprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprintId);

                status = sprint.Status;
                completedAt = sprint.CompletedAt;
            });

            return (status, completedAt);
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Complete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprintId}/complete",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Complete_WhenSprintDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownSprintId = Guid.NewGuid();

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{unknownSprintId}/complete",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Complete_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSD");
            var sprint = await CreateSprintAsync(project.Id, "Developer Complete Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await AuthenticateAsync(
                "complete-developer@test.com",
                "complete-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "complete-developer@test.com",
                ProjectRole.Developer);

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var state = await GetSprintStateAsync(sprint.Id);

            state.Status.Should().Be(SprintStatus.Active);
            state.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task Complete_WhenSprintIsPlanning_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSP");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Planning Sprint");

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var state = await GetSprintStateAsync(sprint.Id);

            state.Status.Should().Be(SprintStatus.Planning);
            state.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task Complete_WhenSprintIsAlreadyCompleted_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSC");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Already Completed Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var firstCompleteResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            firstCompleteResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            var stateBeforeSecondComplete = await GetSprintStateAsync(
                sprint.Id);

            stateBeforeSecondComplete.Status.Should()
                .Be(SprintStatus.Completed);

            stateBeforeSecondComplete.CompletedAt.Should()
                .NotBeNull();

            // Act
            var secondCompleteResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            // Assert
            secondCompleteResponse.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var stateAfterSecondComplete = await GetSprintStateAsync(
                sprint.Id);

            stateAfterSecondComplete.Status.Should()
                .Be(SprintStatus.Completed);

            stateAfterSecondComplete.CompletedAt.Should()
                .Be(stateBeforeSecondComplete.CompletedAt);
        }

        [Fact]
        public async Task Complete_WhenUserIsManagerAndSprintIsActive_ShouldReturnNoContentAndCompleteSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("CSV");

            var sprint = await CreateSprintAsync(
                project.Id,
                "Valid Complete Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var stateBeforeComplete = await GetSprintStateAsync(
                sprint.Id);

            stateBeforeComplete.Status.Should().Be(SprintStatus.Active);
            stateBeforeComplete.CompletedAt.Should().BeNull();

            var beforeComplete = DateTime.UtcNow;

            // Act
            var response = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            var afterComplete = DateTime.UtcNow;

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var state = await GetSprintStateAsync(sprint.Id);

            state.Status.Should().Be(SprintStatus.Completed);
            state.CompletedAt.Should().NotBeNull();

            state.CompletedAt!.Value.Should()
                .BeOnOrAfter(beforeComplete);

            state.CompletedAt.Value.Should()
                .BeOnOrBefore(afterComplete);
        }
    }
}