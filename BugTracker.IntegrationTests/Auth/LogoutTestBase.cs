using BugTracker.Application.DTOs.Auth;
using BugTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Auth
{
    public class LogoutTests : AuthTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public LogoutTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Logout_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var dto = new RefreshTokenDto
            {
                RefreshToken = "some-refresh-token"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/logout",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WhenRefreshTokenDoesNotExist_ShouldReturnUnauthorized()
        {
            await AuthenticateAsync();

            var dto = new RefreshTokenDto
            {
                RefreshToken = "unknown-refresh-token"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/logout",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WhenRefreshTokenIsAlreadyRevoked_ShouldReturnUnauthorized()
        {
            var authResponse =
                await AuthenticateAsync();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken =
                    await dbContext.Set<RefreshToken>()
                        .SingleAsync(
                            r => r.Token ==
                                 authResponse.RefreshToken);

                refreshToken.IsRevoked = true;

                await dbContext.SaveChangesAsync();
            });

            var dto = new RefreshTokenDto
            {
                RefreshToken =
                    authResponse.RefreshToken
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/logout",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WhenRefreshTokenIsValid_ShouldReturnNoContentAndRevokeToken()
        {
            var authResponse =
                await AuthenticateAsync();

            var dto = new RefreshTokenDto
            {
                RefreshToken =
                    authResponse.RefreshToken
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/logout",
                dto);

            response.StatusCode.Should().Be(
                HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken =
                    await dbContext.Set<RefreshToken>()
                        .SingleAsync(
                            r => r.Token ==
                                 authResponse.RefreshToken);

                refreshToken.IsRevoked.Should()
                    .BeTrue();
            });
        }
    }
}