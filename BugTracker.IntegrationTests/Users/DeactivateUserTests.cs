using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class DeactivateUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeactivateUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Deactivate_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/deactivate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Deactivate_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/deactivate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Deactivate_WhenTargetDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "deactivate-admin@test.com",
                "deactivate-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/deactivate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Deactivate_WhenAdminDeactivatesActiveUser_ShouldReturnNoContent()
        {
            var target = await CreateUserAsync(
                "deactivate-target@test.com",
                "deactivate-target");

            await AuthenticateAsync(
                "deactivate-valid-admin@test.com",
                "deactivate-valid-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/users/{target.Id}/deactivate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var isActive = await GetIsActiveAsync(target.Id);

            isActive.Should().BeFalse();
        }
    }
}