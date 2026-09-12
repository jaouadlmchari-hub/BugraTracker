using BugTracker.Application.DTOs.Auth;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using BugTracker.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BugTracker.IntegrationTests
{
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly CustomWebApplicationFactory Factory;
        protected readonly HttpClient Client;

        protected IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;

            Client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await Factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            Client.Dispose();

            return Task.CompletedTask;
        }

        protected async Task ExecuteDbContextAsync(Func<BugTrackerDbContext, Task> action)
        {
            using var scope = Factory.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<BugTrackerDbContext>();

            await action(dbContext);
        }

        protected Task<AuthResponseDto> AuthenticateAsync()
        {
            return AuthenticateAsync("authenticated@test.com", "authenticated-user");
        }

        protected async Task<AuthResponseDto> AuthenticateAsync(string email, string username, SystemRole systemRole = SystemRole.Developer)
        {
            const string password = "Password123!";

            string passwordHash;

            using (var scope = Factory.Services.CreateScope())
            {
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                passwordHash = passwordHasher.Hash(password);
            }

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    SystemRole = systemRole
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();
            });

            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (authResponse == null)
                throw new InvalidOperationException("La réponse d'authentification est vide.");

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            return authResponse;
        }
    }
}