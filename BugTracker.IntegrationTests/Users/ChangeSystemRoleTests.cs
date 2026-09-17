using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class ChangeSystemRoleTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public ChangeSystemRoleTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ChangeSystemRole_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.PatchAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/role",
                SystemRole.Admin);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangeSystemRole_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.PatchAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/role",
                SystemRole.Admin);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ChangeSystemRole_WhenTargetDoesNotExist_ShouldReturnNotFound()
        {
            await AuthenticateAsync(
                "role-admin@test.com",
                "role-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsJsonAsync(
                $"/api/users/{Guid.NewGuid()}/role",
                SystemRole.Admin);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ChangeSystemRole_WhenAdminChangesRole_ShouldReturnNoContentAndPersistRole()
        {
            var target = await CreateUserAsync(
                "role-target@test.com",
                "role-target");

            await AuthenticateAsync(
                "role-valid-admin@test.com",
                "role-valid-admin",
                SystemRole.Admin);

            var response = await Client.PatchAsJsonAsync(
                $"/api/users/{target.Id}/role",
                SystemRole.Admin);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var role = await dbContext.Set<User>()
                    .Where(u => u.Id == target.Id)
                    .Select(u => u.SystemRole)
                    .SingleAsync();

                role.Should().Be(SystemRole.Admin);
            });
        }
    }
}