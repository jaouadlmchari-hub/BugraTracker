using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace BugTracker.IntegrationTests.Auth
{
    public abstract class AuthTestBase : IntegrationTestBase
    {
        protected AuthTestBase(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected string HashPassword(string password)
        {
            using var scope = Factory.Services.CreateScope();

            var passwordHasher =
                scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            return passwordHasher.Hash(password);
        }

        protected async Task<Guid> CreateUserAsync(
            string email,
            string username,
            string password = "Password123!",
            bool isActive = true,
            int failedLoginAttempts = 0,
            DateTime? lockoutUntil = null)
        {
            var userId = Guid.Empty;

            var passwordHash = HashPassword(password);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = passwordHash,
                    IsActive = isActive,
                    FailedLoginAttempts = failedLoginAttempts,
                    LockoutUntil = lockoutUntil
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;
            });

            return userId;
        }

        protected async Task<(Guid UserId, Guid RefreshTokenId)> CreateUserWithRefreshTokenAsync(
            string email,
            string username,
            string refreshTokenValue,
            DateTime expiresAt,
            bool isRevoked = false,
            bool isActive = true)
        {
            var userId = Guid.Empty;
            var refreshTokenId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = "not-used-here",
                    IsActive = isActive
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;

                var refreshToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiresAt = expiresAt,
                    IsRevoked = isRevoked
                };

                dbContext.Set<RefreshToken>().Add(refreshToken);

                await dbContext.SaveChangesAsync();

                refreshTokenId = refreshToken.Id;
            });

            return (userId, refreshTokenId);
        }
    }
}