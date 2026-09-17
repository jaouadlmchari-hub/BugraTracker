using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Projects
{
    public class ChangeOwnerTests : ProjectsTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangeOwnerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ChangeOwner_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new ChangeProjectOwnerDto
            {
                NewOwnerId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{Guid.NewGuid()}/owner",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangeOwner_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync();

            var dto = new ChangeProjectOwnerDto
            {
                NewOwnerId = Guid.NewGuid()
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{Guid.NewGuid()}/owner",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ChangeOwner_WhenUserIsNotOwner_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("POF");

            var target = await CreateUserAsync(
                "new-owner-f@test.com",
                "new-owner-f");

            await AddProjectMemberAsync(
                project.Id,
                target.Id,
                ProjectRole.Developer);

            await AuthenticateAsync(
                "owner-outsider@test.com",
                "owner-outsider");

            var dto = new ChangeProjectOwnerDto
            {
                NewOwnerId = target.Id
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/owner",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ChangeOwner_WhenOwnerTransfersOwnershipToMember_ShouldReturnNoContent()
        {
            await AuthenticateAsync();

            var project = await CreateProjectAsync("POV");

            var newOwner = await CreateUserAsync(
                "new-owner@test.com",
                "new-owner");

            await AddProjectMemberAsync(
                project.Id,
                newOwner.Id,
                ProjectRole.Developer);

            var dto = new ChangeProjectOwnerDto
            {
                NewOwnerId = newOwner.Id
            };

            var response = await Client.PatchAsJsonAsync(
                $"/api/projects/{project.Id}/owner",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await Client.GetAsync(
                $"/api/projects/{project.Id}");

            var result = await getResponse.Content
                .ReadFromJsonAsync<ProjectDto>();

            result.Should().NotBeNull();
            result!.OwnerId.Should().Be(newOwner.Id);
        }
    }
}