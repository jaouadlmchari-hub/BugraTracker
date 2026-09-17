using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class GetAllUsersTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public GetAllUsersTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            var response = await Client.GetAsync("/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WhenUserIsNotAdmin_ShouldReturnForbidden()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync("/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetAll_WhenUserIsAdmin_ShouldReturnPagedResult()
        {
            await CreateUserAsync("page-one@test.com", "page-one");
            await CreateUserAsync("page-two@test.com", "page-two");
            await CreateUserAsync("page-three@test.com", "page-three");

            await AuthenticateAsync(
                "page-admin@test.com",
                "page-admin",
                SystemRole.Admin);

            var response = await Client.GetAsync(
                "/api/users?pageNumber=1&pageSize=2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResultDto<UserDto>>();

            result.Should().NotBeNull();

            result!.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().BeGreaterThanOrEqualTo(4);
        }
    }
}