using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class AdminCreateUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public AdminCreateUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private AdminCreateUserDto ValidDto()
        {
            return new AdminCreateUserDto
            {
                Email = "created-by-admin@test.com",
                Username = "created-by-admin",
                FullName = "Created By Admin",
                Password = "Password123!",
                SystemRole = SystemRole.Admin
            };
        }

        [Fact]
        public async Task AdminCreate_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PostAsJsonAsync(
                "/api/users/admin",
                ValidDto());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AdminCreate_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/users/admin",
                ValidDto());

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AdminCreate_WhenAdminProvidesValidData_ShouldReturnCreated()
        {
            await AuthenticateAsync(
                "creator-admin@test.com",
                "creator-admin",
                SystemRole.Admin);

            var response = await Client.PostAsJsonAsync(
                "/api/users/admin",
                ValidDto());

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();

            user.Should().NotBeNull();
            user!.Email.Should().Be("created-by-admin@test.com");
            user.SystemRole.Should().Be(SystemRole.Admin);
            user.IsActive.Should().BeTrue();
        }
    }
}