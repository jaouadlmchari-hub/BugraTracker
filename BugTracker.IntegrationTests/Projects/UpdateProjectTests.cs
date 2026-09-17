using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class UpdateProjectTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UpdateProjectTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private UpdateProjectDto ValidUpdate()
        {
            return new UpdateProjectDto
            {
                Name = "Updated Project",
                Description = "Updated description"
            };
        }

        [Fact]
        public async Task Update_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PutAsJsonAsync(
                $"/api/projects/{Guid.NewGuid()}",
                ValidUpdate());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_WhenUserCannotManageProject_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PUF");

            await AuthenticateAsync(
                "update-outsider@test.com",
                "update-outsider");

            var response = await Client.PutAsJsonAsync(
                $"/api/projects/{project.Id}",
                ValidUpdate());

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Update_WhenManagerUpdatesProject_ShouldReturnOk()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("PUV");

            var dto = ValidUpdate();

            var response = await Client.PutAsJsonAsync(
                $"/api/projects/{project.Id}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.Id.Should().Be(project.Id);
            result.Name.Should().Be(dto.Name);
            result.Description.Should().Be(dto.Description);
        }

        [Fact]
        public async Task Update_WhenProjectDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "update-project-admin@test.com",
                "update-project-admin",
                SystemRole.Admin);

            var response = await Client.PutAsJsonAsync(
                $"/api/projects/{Guid.NewGuid()}",
                ValidUpdate());

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}