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
    public class ChangeStatusTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangeStatusTests(CustomWebApplicationFactory factory) : base(factory)
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

            var epic = await response.Content
                .ReadFromJsonAsync<EpicDto>();

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

        private async Task<EpicStatus> GetEpicStatusAsync(Guid epicId)
        {
            EpicStatus status = default;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var epic = await dbContext.Set<Epic>()
                    .SingleAsync(e => e.Id == epicId);

                status = epic.Status;
            });

            return status;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task ChangeStatus_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            var dto = new ChangeEpicStatusDto
            {
                NewStatus = EpicStatus.Completed
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/epics/{epicId}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangeStatus_WhenEpicDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var unknownEpicId = Guid.NewGuid();

            var dto = new ChangeEpicStatusDto
            {
                NewStatus = EpicStatus.Completed
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/epics/{unknownEpicId}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ChangeStatus_WhenUserIsDeveloper_ShouldReturnForbidden()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ESD");

            var epic = await CreateEpicAsync(
                project.Id,
                "Developer Status Epic");

            await AuthenticateAsync(
                "epic-status-developer@test.com",
                "epic-status-developer-user");

            await AddExistingUserAsMemberAsync(
                project.Id,
                "epic-status-developer@test.com",
                ProjectRole.Developer);

            var dto = new ChangeEpicStatusDto
            {
                NewStatus = EpicStatus.Completed
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/epics/{epic.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var status = await GetEpicStatusAsync(epic.Id);

            status.Should().Be(EpicStatus.Active);
        }

        [Fact]
        public async Task ChangeStatus_WhenStatusIsInvalid_ShouldReturnUnprocessableEntity()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ESI");

            var epic = await CreateEpicAsync(
                project.Id,
                "Invalid Status Epic");

            var dto = new ChangeEpicStatusDto
            {
                NewStatus = (EpicStatus)999
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/epics/{epic.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.UnprocessableEntity);

            var status = await GetEpicStatusAsync(epic.Id);

            status.Should().Be(EpicStatus.Active);
        }

        [Theory]
        [InlineData(EpicStatus.Active)]
        [InlineData(EpicStatus.Completed)]
        [InlineData(EpicStatus.Archived)]
        public async Task ChangeStatus_WhenUserIsManagerAndStatusIsValid_ShouldReturnNoContentAndChangeStatus(EpicStatus newStatus)
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("ESV");

            var epic = await CreateEpicAsync(
                project.Id,
                "Valid Status Epic");

            var dto = new ChangeEpicStatusDto
            {
                NewStatus = newStatus
            };

            // Act
            var response = await Client.PatchAsJsonAsync(
                $"/api/epics/{epic.Id}/status",
                dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var savedStatus = await GetEpicStatusAsync(epic.Id);

            savedStatus.Should().Be(newStatus);
        }
    }
}