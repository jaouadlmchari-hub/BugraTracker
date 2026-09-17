using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Projects;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class GetAllTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetAllTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync("/api/projects");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WhenUserIsAuthenticated_ShouldReturnOk()
        {
            await AuthenticateAsync();

            await CreateProjectAsync("PGA");
            await CreateProjectAsync("PGB");

            var response = await Client.GetAsync("/api/projects");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<ProjectDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAll_WithPagination_ShouldRespectPageSize()
        {
            await AuthenticateAsync();

            await CreateProjectAsync("PA1");
            await CreateProjectAsync("PA2");
            await CreateProjectAsync("PA3");

            var response = await Client.GetAsync(
                "/api/projects?pageNumber=1&pageSize=2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<ProjectDto>>();

            result.Should().NotBeNull();
            result!.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(3);
        }
    }
}
