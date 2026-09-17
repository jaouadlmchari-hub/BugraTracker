using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class ActivateProjectTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ActivateProjectTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Activate_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PatchAsync(
                $"/api/projects/{Guid.NewGuid()}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Activate_WhenUserCannotManageProject_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PTF");

            await AuthenticateAsync(
                "activate-outsider@test.com",
                "activate-outsider");

            var response = await Client.PatchAsync(
                $"/api/projects/{project.Id}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Activate_WhenManagerActivatesArchivedProject_ShouldReturnNoContent()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PTV");

            var archiveResponse = await Client.PatchAsync(
                $"/api/projects/{project.Id}/archive",
                null);

            archiveResponse.StatusCode.Should().Be(
                HttpStatusCode.NoContent);

            var activateResponse = await Client.PatchAsync(
                $"/api/projects/{project.Id}/activate",
                null);

            activateResponse.StatusCode.Should().Be(
                HttpStatusCode.NoContent);

            var getResponse = await Client.GetAsync(
                $"/api/projects/{project.Id}");

            var result = await getResponse.Content
                .ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.Status.Should().Be(ProjectStatus.Active);
        }

        [Fact]
        public async Task Activate_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "activate-project-admin@test.com",
                "activate-project-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/projects/{Guid.NewGuid()}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}