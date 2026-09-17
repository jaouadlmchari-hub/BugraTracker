using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class DeleteProjectTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeleteProjectTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Delete_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.DeleteAsync(
                $"/api/projects/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PDF");

            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "delete-project-admin@test.com",
                "delete-project-admin",
                SystemRole.Admin);

            var response = await Client.DeleteAsync(
                $"/api/projects/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenAdminDeletesProject_ShouldReturnNoContentAndRemoveProject()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PDV");

            await AuthenticateAsync(
                "delete-admin@test.com",
                "delete-admin",
                SystemRole.Admin);

            var response = await Client.DeleteAsync(
                $"/api/projects/{project.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var exists = await ProjectExistsAsync(project.Id);

            exists.Should().BeFalse();
        }
    }
}