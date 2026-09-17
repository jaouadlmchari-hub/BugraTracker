using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class ActivateUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ActivateUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Activate_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Activate_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Activate_WhenTargetDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "activate-admin@test.com",
                "activate-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/users/{Guid.NewGuid()}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Activate_WhenAdminActivatesInactiveUser_ShouldReturnNoContent()
        {
            var target = await CreateUserAsync(
                "activate-target@test.com",
                "activate-target");

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == target.Id);

                user.IsActive = false;

                await dbContext.SaveChangesAsync();
            });

            await AuthenticateAsync(
                "activate-valid-admin@test.com",
                "activate-valid-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsync(
                $"/api/users/{target.Id}/activate",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var isActive = await GetIsActiveAsync(target.Id);

            isActive.Should().BeTrue();
        }
    }
}