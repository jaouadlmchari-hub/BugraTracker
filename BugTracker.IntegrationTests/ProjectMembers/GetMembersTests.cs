using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.ProjectMembers
{
    public class GetMembersTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetMembersTests(CustomWebApplicationFactory factory) : base(factory)
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

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            var project = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();

            return project!;
        }

        // =========================================================
        // Tests
        // =========================================================

        [Fact]
        public async Task GetMembers_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{projectId}/members");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMembers_WhenUserIsProjectMember_ShouldReturnOkAndMembers()
        {
            // Arrange
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GM");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/members");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var members = await response.Content
                .ReadFromJsonAsync<IEnumerable<ProjectMemberDto>>();

            members.Should().NotBeNull();

            var member = members!.Single();

            member.Username.Should()
                .Be("authenticated-user");

            member.Role.Should()
                .Be(ProjectRole.Manager);
        }

        [Fact]
        public async Task GetMembers_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            // Arrange

            // User 1 crée le projet
            await AuthenticateAsync();

            var project = await CreateProjectAsync("GMO");

            // User 2 devient l'utilisateur courant
            // mais n'est pas ajouté au projet
            await AuthenticateAsync(
                "outsider-members@test.com",
                "outsider-members-user");

            // Act
            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}/members");

            // Assert
            response.StatusCode.Should()
                .Be(HttpStatusCode.Forbidden);
        }
    }
}