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
    public class CreateSprintTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CreateSprintTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<Guid> AddExistingUserAsMemberAsync(Guid projectId, string email, ProjectRole role)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                userId = user.Id;

                var membership = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    Role = role
                };

                dbContext.Set<ProjectMember>().Add(membership);

                await dbContext.SaveChangesAsync();
            });

            return userId;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task Create_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new CreateSprintDto
            {
                Name = "Sprint 1",
                Goal = "Test sprint",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/sprints",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet → Manager
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SCD");

            // User 2 devient utilisateur courant
            await AuthenticateAsync(
                "sprint-developer@test.com",
                "sprint-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "sprint-developer@test.com",
                ProjectRole.Developer);

            var dto = new CreateSprintDto
            {
                Name = "Sprint Developer",
                Goal = "Developer should not create sprint",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/sprints",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var sprintExists = await dbContext.Set<Sprint>()
                    .AnyAsync(s =>
                        s.ProjectId == project.Id &&
                        s.Name == dto.Name);

                sprintExists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "sprint-admin@test.com",
                "sprint-admin-user",
                SystemRole.Admin);

            var unknownProjectId = Guid.NewGuid();

            var dto = new CreateSprintDto
            {
                Name = "Unknown Project Sprint",
                Goal = "Should fail",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{unknownProjectId}/sprints",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WhenDatesAreInvalid_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SCI");

            var dto = new CreateSprintDto
            {
                Name = "Invalid Dates Sprint",
                Goal = "End date is before start date",
                StartDate = DateTime.UtcNow.Date.AddDays(10),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/sprints",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var sprintExists = await dbContext.Set<Sprint>()
                    .AnyAsync(s =>
                        s.ProjectId == project.Id &&
                        s.Name == dto.Name);

                sprintExists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenUserIsManagerAndDataIsValid_ShouldReturnCreatedAndCreateSprint()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("SCV");

            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(14);

            var dto = new CreateSprintDto
            {
                Name = "Sprint 1",
                Goal = "Complete authentication module",
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/sprints",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var sprint = await response.Content
                .ReadFromJsonAsync<SprintDto>();

            sprint.Should().NotBeNull();
            sprint!.ProjectId.Should().Be(project.Id);
            sprint.Name.Should().Be(dto.Name);
            sprint.Goal.Should().Be(dto.Goal);
            sprint.Status.Should().Be(SprintStatus.Planning);
            sprint.StartDate.Should().Be(startDate);
            sprint.EndDate.Should().Be(endDate);
            sprint.CompletedAt.Should().BeNull();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedSprint = await dbContext.Set<Sprint>()
                    .SingleAsync(s => s.Id == sprint.Id);

                savedSprint.ProjectId.Should().Be(project.Id);
                savedSprint.Name.Should().Be(dto.Name);
                savedSprint.Goal.Should().Be(dto.Goal);
                savedSprint.Status.Should().Be(SprintStatus.Planning);
                savedSprint.StartDate.Should().Be(startDate);
                savedSprint.EndDate.Should().Be(endDate);
                savedSprint.CompletedAt.Should().BeNull();
            });
        }
    }
}