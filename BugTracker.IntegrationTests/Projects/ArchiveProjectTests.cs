using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class ArchiveProjectTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ArchiveProjectTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Archive_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PatchAsync(
                $"/api/projects/{Guid.NewGuid()}/archive",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Archive_WhenUserCannotManageProject_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PAF");

            await AuthenticateAsync(
                "archive-outsider@test.com",
                "archive-outsider");

            var response = await Client.PatchAsync(
                $"/api/projects/{project.Id}/archive",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Archive_WhenManagerArchivesActiveProject_ShouldReturnNoContent()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PAV");

            var response = await Client.PatchAsync(
                $"/api/projects/{project.Id}/archive",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await Client.GetAsync(
                $"/api/projects/{project.Id}");

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await getResponse.Content
                .ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.Status.Should().Be(ProjectStatus.Archived);
        }

        [Fact]
        public async Task Archive_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "archive-admin@test.com",
                "archive-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/projects/{Guid.NewGuid()}/archive",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}