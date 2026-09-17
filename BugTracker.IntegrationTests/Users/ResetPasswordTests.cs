using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class ResetPasswordTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ResetPasswordTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ResetPassword_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new ResetPasswordDto
            {
                NewPassword = "ResetPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/reset-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ResetPassword_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var dto = new ResetPasswordDto
            {
                NewPassword = "ResetPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/reset-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ResetPassword_WhenTargetDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "reset-admin@test.com",
                "reset-admin",
                SystemRole.Admin);

            var dto = new ResetPasswordDto
            {
                NewPassword = "ResetPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/reset-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ResetPassword_WhenAdminResetsPassword_ShouldReturnNoContentAndNewPasswordShouldWork()
        {
            const string email = "reset-target@test.com";

            var target = await CreateUserAsync(
                email,
                "reset-target",
                "OldPassword123!");

            await AuthenticateAsync(
                "reset-valid-admin@test.com",
                "reset-valid-admin",
                SystemRole.Admin);

            var dto = new ResetPasswordDto
            {
                NewPassword = "ResetPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                $"/api/users/{target.Id}/reset-password",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var oldLogin = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password = "OldPassword123!"
                });

            oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var newLogin = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password = "ResetPassword123!"
                });

            newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}