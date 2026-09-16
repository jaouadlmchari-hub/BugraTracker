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
    public class UpdateSprintTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UpdateSprintTests(CustomWebApplicationFactory factory) : base(factory)
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

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Update_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var dto = new UpdateSprintDto
            {
                Name = "Updated Sprint",
                Goal = "Updated goal",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{sprintId}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_WhenSprintDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownSprintId = Guid.NewGuid();

            var dto = new UpdateSprintDto
            {
                Name = "Unknown Sprint",
                Goal = "Should not be updated",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{unknownSprintId}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SUD");
            var sprint = await CreateSprintAsync(project.Id, "Original Sprint");

            await AuthenticateAsync(
                "update-developer@test.com",
                "update-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "update-developer@test.com",
                ProjectRole.Developer);

            var dto = new UpdateSprintDto
            {
                Name = "Unauthorized Update",
                Goal = "Developer should not update sprint",
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(15)
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{sprint.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedSprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprint.Id);

                savedSprint.Name.Should().Be("Original Sprint");
                savedSprint.Status.Should().Be(SprintStatus.Planning);
            });
        }

        [Fact]
        public async Task Update_WhenSprintIsActive_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SUA");
            var sprint = await CreateSprintAsync(project.Id, "Active Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var dto = new UpdateSprintDto
            {
                Name = "Updated Active Sprint",
                Goal = "Should not be changed",
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(15)
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{sprint.Id}",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedSprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprint.Id);

                savedSprint.Name.Should().Be("Active Sprint");
                savedSprint.Status.Should().Be(SprintStatus.Active);
            });
        }

        [Fact]
        public async Task Update_WhenSprintIsCompleted_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SUC");
            var sprint = await CreateSprintAsync(project.Id, "Completed Sprint");

            var startResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/start",
                null);

            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var completeResponse = await Client.PatchAsync(
                $"/api/sprints/{sprint.Id}/complete",
                null);

            completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var dto = new UpdateSprintDto
            {
                Name = "Updated Completed Sprint",
                Goal = "Should not be changed",
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(15)
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{sprint.Id}",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedSprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprint.Id);

                savedSprint.Name.Should().Be("Completed Sprint");
                savedSprint.Status.Should().Be(SprintStatus.Completed);
            });
        }

        [Fact]
        public async Task Update_WhenDatesAreInvalid_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SUI");
            var sprint = await CreateSprintAsync(project.Id, "Invalid Date Sprint");

            var dto = new UpdateSprintDto
            {
                Name = "Invalid Date Sprint Updated",
                Goal = "Invalid dates",
                StartDate = DateTime.UtcNow.Date.AddDays(20),
                EndDate = DateTime.UtcNow.Date.AddDays(10)
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{sprint.Id}",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedSprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprint.Id);

                savedSprint.Name.Should().Be("Invalid Date Sprint");
                savedSprint.Status.Should().Be(SprintStatus.Planning);
            });
        }

        [Fact]
        public async Task Update_WhenUserIsManagerAndSprintIsPlanning_ShouldReturnOkAndUpdateSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SUV");
            var sprint = await CreateSprintAsync(project.Id, "Original Sprint");

            var newStartDate = DateTime.UtcNow.Date.AddDays(2);
            var newEndDate = newStartDate.AddDays(21);

            var dto = new UpdateSprintDto
            {
                Name = "Updated Sprint",
                Goal = "Updated integration test goal",
                StartDate = newStartDate,
                EndDate = newEndDate
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/sprints/{sprint.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedSprint = await response.Content
                .ReadFromJsonAsync<SprintDto>();

            updatedSprint.Should().NotBeNull();

            updatedSprint!.Id.Should().Be(sprint.Id);
            updatedSprint.ProjectId.Should().Be(project.Id);
            updatedSprint.Name.Should().Be(dto.Name);
            updatedSprint.Goal.Should().Be(dto.Goal);
            updatedSprint.StartDate.Should().Be(newStartDate);
            updatedSprint.EndDate.Should().Be(newEndDate);
            updatedSprint.Status.Should().Be(SprintStatus.Planning);
            updatedSprint.CompletedAt.Should().BeNull();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedSprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprint.Id);

                savedSprint.Name.Should().Be(dto.Name);
                savedSprint.Goal.Should().Be(dto.Goal);
                savedSprint.StartDate.Should().Be(newStartDate);
                savedSprint.EndDate.Should().Be(newEndDate);
                savedSprint.Status.Should().Be(SprintStatus.Planning);
            });
        }
    }
}