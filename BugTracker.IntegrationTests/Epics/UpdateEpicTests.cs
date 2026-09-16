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
    public class UpdateEpicTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UpdateEpicTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task Update_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            var dto = new UpdateEpicDto
            {
                Title = "Updated Epic",
                Description = "Updated description",
                ColorCode = "#22C55E"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/epics/{epicId}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_WhenEpicDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownEpicId = Guid.NewGuid();

            var dto = new UpdateEpicDto
            {
                Title = "Unknown Epic",
                Description = "Should not be updated",
                ColorCode = "#22C55E"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/epics/{unknownEpicId}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EUD");

            var epic = await CreateEpicAsync(
                project.Id,
                "Original Epic");

            await AuthenticateAsync(
                "update-epic-developer@test.com",
                "update-epic-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "update-epic-developer@test.com",
                ProjectRole.Developer);

            var dto = new UpdateEpicDto
            {
                Title = "Unauthorized Update",
                Description = "Developer should not update this Epic",
                ColorCode = "#FF0000"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/epics/{epic.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedEpic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epic.Id);

                savedEpic.Title.Should().Be("Original Epic");
                savedEpic.Description.Should().Be("Description for Original Epic");
                savedEpic.ColorCode.Should().Be("#3B82F6");
                savedEpic.Status.Should().Be(EpicStatus.Active);
            });
        }

        [Fact]
        public async Task Update_WhenEpicIsArchived_ShouldReturnUnprocessableEntityAndKeepEpicUnchanged()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EUA");

            var epic = await CreateEpicAsync(
                project.Id,
                "Archived Epic");

            await SetEpicStatusAsync(
                epic.Id,
                EpicStatus.Archived);

            var dto = new UpdateEpicDto
            {
                Title = "Modified Archived Epic",
                Description = "This modification must not happen",
                ColorCode = "#FF0000"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/epics/{epic.Id}",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var savedEpic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epic.Id);

                savedEpic.Title.Should().Be("Archived Epic");
                savedEpic.Description.Should().Be("Description for Archived Epic");
                savedEpic.ColorCode.Should().Be("#3B82F6");
                savedEpic.Status.Should().Be(EpicStatus.Archived);
            });
        }

        [Fact]
        public async Task Update_WhenUserIsManagerAndEpicIsActive_ShouldReturnOkAndUpdateEpic()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("EUV");

            var epic = await CreateEpicAsync(
                project.Id,
                "Original Active Epic");

            var dto = new UpdateEpicDto
            {
                Title = "Updated Active Epic",
                Description = "Updated Epic description",
                ColorCode = "#22C55E"
            };

            // Act
            var response = await Client.PutAsJsonAsync(
                $"/api/epics/{epic.Id}",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedEpic = await response.Content
                .ReadFromJsonAsync<EpicDto>();

            updatedEpic.Should().NotBeNull();

            updatedEpic!.Id.Should().Be(epic.Id);
            updatedEpic.ProjectId.Should().Be(project.Id);
            updatedEpic.Title.Should().Be(dto.Title);
            updatedEpic.Description.Should().Be(dto.Description);
            updatedEpic.ColorCode.Should().Be(dto.ColorCode);
            updatedEpic.Status.Should().Be(EpicStatus.Active);

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