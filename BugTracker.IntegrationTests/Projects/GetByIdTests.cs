using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class GetByIdTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIdTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/projects/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PGF");

            await AuthenticateAsync(
                "project-outsider@test.com",
                "project-outsider");

            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetById_WhenUserIsProjectMember_ShouldReturnOk()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PGV");

            var response = await Client.GetAsync(
                $"/api/projects/{project.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.Id.Should().Be(project.Id);
            result.Key.Should().Be("PGV");
        }

        [Fact]
        public async Task GetById_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "project-admin@test.com",
                "project-admin",
                SystemRole.Admin);

            var response = await Client.GetAsync(
                $"/api/projects/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}