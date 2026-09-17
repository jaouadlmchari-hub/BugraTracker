using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests.Users
{
    public class CreateUserTests : UsersTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CreateUserTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_WhenDataIsValid_ShouldReturnCreatedAndCreateDeveloper()
        {
            var dto = new CreateUserDto
            {
                Email = "register@test.com",
                Username = "register-user",
                FullName = "Register User",
                Password = "Password123!"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var user = await response.Content
                .ReadFromJsonAsync<UserDto>();

            user.Should().NotBeNull();

            user!.Email.Should().Be(dto.Email);
            user.Username.Should().Be(dto.Username);
            user.FullName.Should().Be(dto.FullName);
            user.SystemRole.Should().Be(SystemRole.Developer);
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Create_WhenEmailIsInvalid_ShouldReturnBadRequest()
        {
            var dto = new CreateUserDto
            {
                Email = "not-an-email",
                Username = "invalid-email",
                FullName = "Invalid Email",
                Password = "Password123!"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WhenPasswordIsTooShort_ShouldReturnBadRequest()
        {
            var dto = new CreateUserDto
            {
                Email = "short-password@test.com",
                Username = "short-password",
                FullName = "Short Password",
                Password = "123"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WhenEmailAlreadyExists_ShouldReturnConflict()
        {
            await CreateUserAsync(
                "duplicate-email@test.com",
                "first-duplicate");

            var dto = new CreateUserDto
            {
                Email = "duplicate-email@test.com",
                Username = "second-duplicate",
                FullName = "Duplicate Email",
                Password = "Password123!"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_WhenUsernameAlreadyExists_ShouldReturnConflict()
        {
            await CreateUserAsync(
                "first-username@test.com",
                "same-username");

            var dto = new CreateUserDto
            {
                Email = "second-username@test.com",
                Username = "same-username",
                FullName = "Duplicate Username",
                Password = "Password123!"
            };

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}