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
    public class CreateEpicTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CreateEpicTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task Create_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new CreateEpicDto
            {
                Title = "Authentication Epic",
                Description = "Authentication features",
                ColorCode = "#3B82F6"
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/epics",
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

            var project = await CreateProjectAsync("ECD");

            // User 2 devient utilisateur courant
            await AuthenticateAsync(
                "epic-developer@test.com",
                "epic-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "epic-developer@test.com",
                ProjectRole.Developer);

            var dto = new CreateEpicDto
            {
                Title = "Forbidden Epic",
                Description = "Developer should not create this epic",
                ColorCode = "#FF0000"
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/epics",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var epicExists = await dbContext.Set<Epic>()
                    .AnyAsync(e =>
                        e.ProjectId == project.Id &&
                        e.Title == dto.Title);

                epicExists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Create_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync(
                "epic-admin@test.com",
                "epic-admin-user",
                SystemRole.Admin);

            var unknownProjectId = Guid.NewGuid();

            var dto = new CreateEpicDto
            {
                Title = "Unknown Project Epic",
                Description = "Project does not exist",
                ColorCode = "#3B82F6"
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{unknownProjectId}/epics",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WhenUserIsManagerAndDataIsValid_ShouldReturnCreatedAndCreateEpic()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ECV");

            var dto = new CreateEpicDto
            {
                Title = "Authentication",
                Description = "Implement authentication and authorization",
                ColorCode = "#FF5733"
            };

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/epics",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var epic = await response.Content
                .ReadFromJsonAsync<EpicDto>();

            epic.Should().NotBeNull();

            epic!.Id.Should().NotBe(Guid.Empty);
            epic.ProjectId.Should().Be(project.Id);
            epic.Title.Should().Be(dto.Title);
            epic.Description.Should().Be(dto.Description);
            epic.ColorCode.Should().Be(dto.ColorCode);
            epic.Status.Should().Be(EpicStatus.Active);

            var exists = await EpicExistsAsync(epic.Id);

            exists.Should().BeTrue();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedEpic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epic.Id);

                savedEpic.ProjectId.Should().Be(project.Id);
                savedEpic.Title.Should().Be(dto.Title);
                savedEpic.Description.Should().Be(dto.Description);
                savedEpic.ColorCode.Should().Be(dto.ColorCode);
                savedEpic.Status.Should().Be(EpicStatus.Active);
            });
        }
    }
}