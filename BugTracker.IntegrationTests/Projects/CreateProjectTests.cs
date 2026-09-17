using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class CreateProjectTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CreateProjectTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new CreateProjectDto
            {
                Name = "Unauthorized Project",
                Key = "CPU"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WhenDataIsValid_ShouldReturnCreated()
        {
            await AuthenticateAsync();

            var dto = new CreateProjectDto
            {
                Name = "Integration Project",
                Key = "CPV",
                Description = "Integration test project"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var project = await response.Content
                .ReadFromJsonAsync<ProjectDto>();

            project.Should().NotBeNull();

            project!.Id.Should().NotBe(Guid.Empty);
            project.Name.Should().Be(dto.Name);
            project.Key.Should().Be(dto.Key);
            project.Description.Should().Be(dto.Description);
            project.Status.Should().Be(ProjectStatus.Active);
            project.OwnerId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Create_WhenKeyAlreadyExists_ShouldReturnConflict()
        {
            await AuthenticateAsync();

            await CreateProjectAsync("DUP");

            var dto = new CreateProjectDto
            {
                Name = "Duplicate Project",
                Key = "DUP"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/projects",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}