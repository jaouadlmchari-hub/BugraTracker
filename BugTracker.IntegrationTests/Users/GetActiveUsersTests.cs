using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class GetActiveUsersTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetActiveUsersTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetActiveUsers_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync("/api/users/active");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetActiveUsers_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync("/api/users/active");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetActiveUsers_WhenUserIsAdmin_ShouldReturnOnlyActiveUsers()
        {
            var activeUser = await CreateUserAsync(
                "active-list@test.com",
                "active-list");

            var inactiveUser = await CreateUserAsync(
                "inactive-list@test.com",
                "inactive-list");

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == inactiveUser.Id);

                user.IsActive = false;

                await dbContext.SaveChangesAsync();
            });

            await AuthenticateAsync(
                "active-admin@test.com",
                "active-admin",
                SystemRole.Admin);

            var response = await Client.GetAsync("/api/users/active");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var users = await response.Content
                .ReadFromJsonAsync<IEnumerable<UserDto>>();

            users.Should().NotBeNull();

            users!.Should().Contain(u => u.Id == activeUser.Id);
            users.Should().NotContain(u => u.Id == inactiveUser.Id);
            users.Should().OnlyContain(u => u.IsActive);
        }
    }
}