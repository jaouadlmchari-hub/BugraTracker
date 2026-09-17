using BugTracker.Application.DTOs.Auth;
using BugTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Auth
{
    public class LoginTests : AuthTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public LoginTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Login_WhenCredentialsAreInvalid_ShouldReturnUnauthorized()
        {
            var dto = new LoginDto
            {
                Email = "unknown@test.com",
                Password = "Password123!"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WhenCredentialsAreValid_ShouldReturnOkAndTokens()
        {
            const string password = "Password123!";

            var userId = await CreateUserAsync(
                "jaouad@test.com",
                "jaouad",
                password,
                isActive: true,
                failedLoginAttempts: 3,
                lockoutUntil: DateTime.UtcNow.AddMinutes(-5));

            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = password
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<AuthResponseDto>();

            result.Should().NotBeNull();

            result!.AccessToken.Should()
                .NotBeNullOrWhiteSpace();

            result.RefreshToken.Should()
                .NotBeNullOrWhiteSpace();

            result.ExpiresAt.Should()
                .BeAfter(DateTime.UtcNow);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == userId);

                user.FailedLoginAttempts.Should().Be(0);
                user.LockoutUntil.Should().BeNull();

                var refreshToken = await dbContext
                    .Set<RefreshToken>()
                    .SingleOrDefaultAsync(
                        r => r.Token == result.RefreshToken);

                refreshToken.Should().NotBeNull();
                refreshToken!.UserId.Should().Be(userId);
                refreshToken.IsRevoked.Should().BeFalse();
                refreshToken.ExpiresAt.Should()
                    .BeAfter(DateTime.UtcNow);
            });
        }

        [Fact]
        public async Task Login_WhenUserIsInactive_ShouldReturnUnauthorized()
        {
            const string password = "Password123!";

            await CreateUserAsync(
                "inactive@test.com",
                "inactive-user",
                password,
                isActive: false);

            var dto = new LoginDto
            {
                Email = "inactive@test.com",
                Password = password
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WhenUserIsLocked_ShouldReturnUnauthorized()
        {
            const string password = "Password123!";

            await CreateUserAsync(
                "locked@test.com",
                "locked-user",
                password,
                isActive: true,
                failedLoginAttempts: 5,
                lockoutUntil: DateTime.UtcNow.AddMinutes(10));

            var dto = new LoginDto
            {
                Email = "locked@test.com",
                Password = password
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WhenPasswordIsIncorrect_ShouldReturnUnauthorizedAndIncrementFailedLoginAttempts()
        {
            const string correctPassword = "Password123!";

            var userId = await CreateUserAsync(
                "wrongpassword@test.com",
                "wrong-password-user",
                correctPassword,
                isActive: true,
                failedLoginAttempts: 2);

            var dto = new LoginDto
            {
                Email = "wrongpassword@test.com",
                Password = "WrongPassword123!"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == userId);

                user.FailedLoginAttempts.Should().Be(3);
                user.LockoutUntil.Should().BeNull();
            });
        }

        [Fact]
        public async Task Login_WhenFailedAttemptsReachLimit_ShouldReturnUnauthorizedAndLockUser()
        {
            const string correctPassword = "Password123!";

            var userId = await CreateUserAsync(
                "lockout@test.com",
                "lockout-user",
                correctPassword,
                isActive: true,
                failedLoginAttempts: 4);

            var dto = new LoginDto
            {
                Email = "lockout@test.com",
                Password = "WrongPassword123!"
            };

            var beforeRequest = DateTime.UtcNow;

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                dto);

            var afterRequest = DateTime.UtcNow;

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == userId);

                user.FailedLoginAttempts.Should().Be(5);

                user.LockoutUntil.Should().NotBeNull();

                user.LockoutUntil!.Value.Should()
                    .BeOnOrAfter(
                        beforeRequest.AddMinutes(15));

                user.LockoutUntil.Value.Should()
                    .BeOnOrBefore(
                        afterRequest.AddMinutes(15));
            });
        }
    }
}