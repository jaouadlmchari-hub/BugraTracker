using BugTracker.Application.DTOs.Projects;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class GetByKeyTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByKeyTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetByKey_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                "/api/projects/key/ABC");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetByKey_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                "/api/projects/key/NONE");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByKey_WhenUserIsNotProjectMember_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            await CreateProjectAsync("PKF");

            await AuthenticateAsync(
                "key-outsider@test.com",
                "key-outsider");

            var response = await Client.GetAsync(
                "/api/projects/key/PKF");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByKey_WhenUserIsProjectMember_ShouldReturnOk()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PKV");

            var response = await Client.GetAsync(
                "/api/projects/key/PKV");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.Id.Should().Be(project.Id);
            result.Key.Should().Be("PKV");
        }
    }
}