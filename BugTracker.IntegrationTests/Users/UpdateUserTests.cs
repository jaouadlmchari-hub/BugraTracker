using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class UpdateUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UpdateUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private UpdateUserDto ValidUpdate(string email)
        {
            return new UpdateUserDto
            {
                Email = email,
                Username = "updated-user",
                FullName = "Updated User"
            };
        }

        [Fact]
        public async Task Update_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PutAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}",
                ValidUpdate("updated@test.com"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_WhenUserTriesToUpdateAnotherUser_ShouldReturnForbidden()
        {
            var target = await CreateUserAsync(
                "update-target@test.com",
                "update-target");

            await AuthenticateAsync(
                "update-other@test.com",
                "update-other");

            var response = await Client.PutAsJsonAsync(
                $"/api/users/{target.Id}",
                ValidUpdate("changed-target@test.com"));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Update_WhenUserUpdatesSelf_ShouldReturnOkAndPersistChanges()
        {
            await AuthenticateAsync(
                "self-update@test.com",
                "self-update");

            var userId = await GetUserIdByEmailAsync(
                "self-update@test.com");

            var dto = new UpdateUserDto
            {
                Email = "self-updated@test.com",
                Username = "self-updated",
                FullName = "Self Updated"
            };

            var response = await Client.PutAsJsonAsync(
                $"/api/users/{userId}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();

            user.Should().NotBeNull();
            user!.Email.Should().Be(dto.Email);
            user.Username.Should().Be(dto.Username);
            user.FullName.Should().Be(dto.FullName);
        }

        [Fact]
        public async Task Update_WhenAdminUpdatesAnotherUser_ShouldReturnOk()
        {
            var target = await CreateUserAsync(
                "admin-update-target@test.com",
                "admin-update-target");

            await AuthenticateAsync(
                "update-admin@test.com",
                "update-admin",
                SystemRole.Admin);

            var dto = ValidUpdate("admin-updated@test.com");

            var response = await Client.PutAsJsonAsync(
                $"/api/users/{target.Id}",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Update_WhenTargetDoesNotExistAndUserIsAdmin_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "update-notfound-admin@test.com",
                "update-notfound-admin",
                SystemRole.Admin);

            var response = await Client.PutAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}",
                ValidUpdate("nothing@test.com"));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}