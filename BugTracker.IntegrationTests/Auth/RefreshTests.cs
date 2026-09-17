using BugTracker.Application.DTOs.Auth;
using BugTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Auth
{
    public class RefreshTests : AuthTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public RefreshTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenDoesNotExist_ShouldReturnUnauthorized()
        {
            var dto = new RefreshTokenDto
            {
                RefreshToken = "unknown-refresh-token"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/refresh",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsRevoked_ShouldReturnUnauthorized()
        {
            const string refreshTokenValue =
                "revoked-refresh-token";

            await CreateUserWithRefreshTokenAsync(
                "revoked@test.com",
                "revoked-user",
                refreshTokenValue,
                DateTime.UtcNow.AddDays(5),
                isRevoked: true);

            var dto = new RefreshTokenDto
            {
                RefreshToken = refreshTokenValue
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/refresh",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsExpired_ShouldReturnUnauthorized()
        {
            const string refreshTokenValue =
                "expired-refresh-token";

            await CreateUserWithRefreshTokenAsync(
                "expired@test.com",
                "expired-user",
                refreshTokenValue,
                DateTime.UtcNow.AddMinutes(-5));

            var dto = new RefreshTokenDto
            {
                RefreshToken = refreshTokenValue
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/refresh",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken =
                    await dbContext.Set<RefreshToken>()
                        .SingleAsync(
                            r => r.Token == refreshTokenValue);

                refreshToken.IsRevoked.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Refresh_WhenUserIsInactive_ShouldReturnUnauthorized()
        {
            const string refreshTokenValue =
                "inactive-user-refresh-token";

            await CreateUserWithRefreshTokenAsync(
                "inactive-refresh@test.com",
                "inactive-refresh-user",
                refreshTokenValue,
                DateTime.UtcNow.AddDays(5),
                isRevoked: false,
                isActive: false);

            var dto = new RefreshTokenDto
            {
                RefreshToken = refreshTokenValue
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/refresh",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken =
                    await dbContext.Set<RefreshToken>()
                        .SingleAsync(
                            r => r.Token == refreshTokenValue);

                refreshToken.IsRevoked.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsValid_ShouldReturnOkAndRotateRefreshToken()
        {
            const string oldRefreshTokenValue =
                "old-valid-refresh-token";

            var resultSetup =
                await CreateUserWithRefreshTokenAsync(
                    "refresh-success@test.com",
                    "refresh-success-user",
                    oldRefreshTokenValue,
                    DateTime.UtcNow.AddDays(5));

            var dto = new RefreshTokenDto
            {
                RefreshToken = oldRefreshTokenValue
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/refresh",
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

            result.RefreshToken.Should()
                .NotBe(oldRefreshTokenValue);

            result.ExpiresAt.Should()
                .BeAfter(DateTime.UtcNow);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var oldRefreshToken =
                    await dbContext.Set<RefreshToken>()
                        .SingleAsync(
                            r => r.Id == resultSetup.RefreshTokenId);

                oldRefreshToken.IsRevoked.Should()
                    .BeTrue();

                var newRefreshToken =
                    await dbContext.Set<RefreshToken>()
                        .SingleOrDefaultAsync(
                            r => r.Token == result.RefreshToken);

                newRefreshToken.Should().NotBeNull();

                newRefreshToken!.UserId.Should()
                    .Be(resultSetup.UserId);

                newRefreshToken.IsRevoked.Should()
                    .BeFalse();

                newRefreshToken.ExpiresAt.Should()
                    .BeAfter(DateTime.UtcNow);
            });
        }
    }
}