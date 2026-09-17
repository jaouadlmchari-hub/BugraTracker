using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class UnlockUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UnlockUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Unlock_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/unlock",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Unlock_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/unlock",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Unlock_WhenTargetDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "unlock-admin@test.com",
                "unlock-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/unlock",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Unlock_WhenUserIsLocked_ShouldReturnNoContentAndResetLockout()
        {
            var target = await CreateUserAsync(
                "locked-target@test.com",
                "locked-target");

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == target.Id);

                user.FailedLoginAttempts = 5;
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);

                await dbContext.SaveChangesAsync();
            });

            await AuthenticateAsync(
                "unlock-valid-admin@test.com",
                "unlock-valid-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/users/{target.Id}/unlock",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == target.Id);

                user.FailedLoginAttempts.Should().Be(0);
                user.LockoutUntil.Should().BeNull();
            });
        }
    }
}