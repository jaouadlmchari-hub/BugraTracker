using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.ProjectMembers
{
    public class GetMemberTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetMemberTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<Guid> GetUserIdByEmailAsync(string email)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Email == email);

                userId = user.Id;
            });

            return userId;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetMember_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{projectId}/members/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMember_WhenMemberExists_ShouldReturnOk()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GME");

            var userId = await GetUserIdByEmailAsync(
                "authenticated@test.com");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/members/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var member = await response.Content
                .ReadFromJsonAsync<ProjectMemberDto>();

            member.Should().NotBeNull();
            member!.UserId.Should().Be(userId);
            member.Username.Should().Be("authenticated-user");
            member.Role.Should().Be(ProjectRole.Manager);
        }

        [Fact]
        public async Task GetMember_WhenMemberDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GMN");

            var unknownUserId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/members/{unknownUserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetMember_WhenCurrentUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GMF");

            var projectMemberId = await GetUserIdByEmailAsync(
                "authenticated@test.com");

            // User 2 devient l'utilisateur courant
            await AuthenticateAsync(
                "outsider-get-member@test.com",
                "outsider-get-member-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/members/{projectMemberId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}