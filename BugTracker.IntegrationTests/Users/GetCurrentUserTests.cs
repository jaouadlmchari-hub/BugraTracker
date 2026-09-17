using BugTracker.Application.DTOs.Users;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class GetCurrentUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetCurrentUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetCurrentUser_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentUser_WhenUserIsAuthenticated_ShouldReturnCurrentUser()
        {
            await AuthenticateAsync(
                "current-user@test.com",
                "current-user");

            var response = await Client.GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();

            user.Should().NotBeNull();
            user!.Email.Should().Be("current-user@test.com");
            user.Username.Should().Be("current-user");
            user.SystemRole.Should().Be(BugTracker.Domain.Enums.SystemRole.Developer);
            user.IsActive.Should().BeTrue();
        }
    }
}