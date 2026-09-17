using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BugTracker.IntegrationTests.Users
{
    public abstract class UsersTestBase : IntegrationTestBase
    {
        protected UsersTestBase(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected async Task<Guid> GetUserIdByEmailAsync(string email)
        {
            Guid userId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                userId = await dbContext.Set<User>()
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .SingleAsync();
            });

            return userId;
        }

        protected async Task<UserDto> CreateUserAsync(string email, string username, string password = "Password123!")
        {
            var dto = new CreateUserDto
            {
                Email = email,
                Username = username,
                FullName = $"Full Name {username}",
                Password = password
            };

            var response = await Client.PostAsJsonAsync(
                "/api/users",
                dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();

            user.Should().NotBeNull();

            return user!;
        }

        protected async Task<bool> GetIsActiveAsync(Guid userId)
        {
            var isActive = false;

            await ExecuteDbContextAsync(async dbContext =>
            {
                isActive = await dbContext.Set<User>()
                    .Where(u => u.Id == userId)
                    .Select(u => u.IsActive)
                    .SingleAsync();
            });

            return isActive;
        }
    }
}