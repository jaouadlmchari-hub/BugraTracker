using BugTracker.Application.DTOs.Users;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class ChangePasswordTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangePasswordTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ChangePassword_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/change-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangePassword_WhenUserTargetsAnotherUser_ShouldReturnForbidden()
        {
            var target = await CreateUserAsync(
                "password-target@test.com",
                "password-target");

            await AuthenticateAsync(
                "password-other@test.com",
                "password-other");

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{target.Id}/change-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ChangePassword_WhenConfirmationDoesNotMatch_ShouldReturnBadRequest()
        {
            await AuthenticateAsync(
                "password-confirm@test.com",
                "password-confirm");

            var userId = await GetUserIdByEmailAsync(
                "password-confirm@test.com");

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "DifferentPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{userId}/change-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ChangePassword_WhenCurrentPasswordIsCorrect_ShouldReturnNoContentAndUseNewPassword()
        {
            const string email = "password-valid@test.com";

            await AuthenticateAsync(
                email,
                "password-valid");

            var userId = await GetUserIdByEmailAsync(email);

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{userId}/change-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var oldLogin = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password = "Password123!"
                });

            oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var newLogin = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password = "NewPassword123!"
                });

            newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}