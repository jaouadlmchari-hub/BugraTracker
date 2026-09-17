using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class GetByIdTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetByIdTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetById_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync(
                $"/api/users/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenUserRequestsSelf_ShouldReturnOk()
        {
            await AuthenticateAsync(
                "user-self@test.com",
                "user-self");

            var userId = await GetUserIdByEmailAsync(
                "user-self@test.com");

            var response = await Client.GetAsync(
                $"/api/users/{userId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();

            user.Should().NotBeNull();
            user!.Id.Should().Be(userId);
            user.Email.Should().Be("user-self@test.com");
        }

        [Fact]
        public async Task GetById_WhenUserHasNoPermission_ShouldReturnForbidden()
        {
            var target = await CreateUserAsync(
                "target-view@test.com",
                "target-view");

            await AuthenticateAsync(
                "viewer@test.com",
                "viewer");

            var response = await Client.GetAsync(
                $"/api/users/{target.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetById_WhenAdminRequestsExistingUser_ShouldReturnOk()
        {
            var target = await CreateUserAsync(
                "admin-target@test.com",
                "admin-target");

            await AuthenticateAsync(
                "view-admin@test.com",
                "view-admin",
                SystemRole.Admin);

            var response = await Client.GetAsync(
                $"/api/users/{target.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_WhenUserDoesNotExistAndCurrentUserIsAdmin_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "notfound-admin@test.com",
                "notfound-admin",
                SystemRole.Admin);

            var response = await Client.GetAsync(
                $"/api/users/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}