using BugTracker.Application.DTOs.Auth;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace BugTracker.IntegrationTests
{
    public class AuthControllerTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Login_WhenCredentialsAreInvalid_ShouldReturnUnauthorized()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "unknown@test.com",
                Password = "Password123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WhenCredentialsAreValid_ShouldReturnOkAndTokens()
        {
            // Arrange
            Guid userId = Guid.Empty;

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
                    Email = "jaouad@test.com",
                    Username = "jaouad",
                    PasswordHash = passwordHash,
                    IsActive = true,
                    FailedLoginAttempts = 3,
                    LockoutUntil = DateTime.UtcNow.AddMinutes(-5)
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;
            });

            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = password
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            result.Should().NotBeNull();
            result!.AccessToken.Should().NotBeNullOrWhiteSpace();
            result.RefreshToken.Should().NotBeNullOrWhiteSpace();
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == userId);

                user.FailedLoginAttempts.Should().Be(0);
                user.LockoutUntil.Should().BeNull();

                var refreshToken = await dbContext.Set<RefreshToken>()
                    .SingleOrDefaultAsync(r => r.Token == result.RefreshToken);

                refreshToken.Should().NotBeNull();
                refreshToken!.UserId.Should().Be(userId);
                refreshToken.IsRevoked.Should().BeFalse();
                refreshToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            });
        }
        [Fact]
        public async Task Login_WhenUserIsInactive_ShouldReturnUnauthorized()
        {
            // Arrange
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
                    Email = "inactive@test.com",
                    Username = "inactive-user",
                    PasswordHash = passwordHash,
                    IsActive = false
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();
            });

            var dto = new LoginDto
            {
                Email = "inactive@test.com",
                Password = password
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WhenUserIsLocked_ShouldReturnUnauthorized()
        {
            // Arrange
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
                    Email = "locked@test.com",
                    Username = "locked-user",
                    PasswordHash = passwordHash,
                    IsActive = true,
                    FailedLoginAttempts = 5,
                    LockoutUntil = DateTime.UtcNow.AddMinutes(10)
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();
            });

            var dto = new LoginDto
            {
                Email = "locked@test.com",
                Password = password
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WhenPasswordIsIncorrect_ShouldReturnUnauthorizedAndIncrementFailedLoginAttempts()
        {
            // Arrange
            const string correctPassword = "Password123!";

            string passwordHash;
            Guid userId = Guid.Empty;

            using (var scope = Factory.Services.CreateScope())
            {
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                passwordHash = passwordHasher.Hash(correctPassword);
            }

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = "wrongpassword@test.com",
                    Username = "wrong-password-user",
                    PasswordHash = passwordHash,
                    IsActive = true,
                    FailedLoginAttempts = 2,
                    LockoutUntil = null
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;
            });

            var dto = new LoginDto
            {
                Email = "wrongpassword@test.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == userId);

                user.FailedLoginAttempts.Should().Be(3);
                user.LockoutUntil.Should().BeNull();
            });
        }

        [Fact]
        public async Task Login_WhenFailedAttemptsReachLimit_ShouldReturnUnauthorizedAndLockUser()
        {
            // Arrange
            const string correctPassword = "Password123!";

            string passwordHash;
            Guid userId = Guid.Empty;

            using (var scope = Factory.Services.CreateScope())
            {
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                passwordHash = passwordHasher.Hash(correctPassword);
            }

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = "lockout@test.com",
                    Username = "lockout-user",
                    PasswordHash = passwordHash,
                    IsActive = true,
                    FailedLoginAttempts = 4,
                    LockoutUntil = null
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;
            });

            var dto = new LoginDto
            {
                Email = "lockout@test.com",
                Password = "WrongPassword123!"
            };

            var beforeRequest = DateTime.UtcNow;

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

            var afterRequest = DateTime.UtcNow;

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Set<User>()
                    .SingleAsync(u => u.Id == userId);

                user.FailedLoginAttempts.Should().Be(5);

                user.LockoutUntil.Should().NotBeNull();

                user.LockoutUntil!.Value.Should()
                    .BeOnOrAfter(beforeRequest.AddMinutes(15));

                user.LockoutUntil.Value.Should()
                    .BeOnOrBefore(afterRequest.AddMinutes(15));
            });
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenDoesNotExist_ShouldReturnUnauthorized()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "unknown-refresh-token"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsRevoked_ShouldReturnUnauthorized()
        {
            // Arrange
            const string refreshTokenValue = "revoked-refresh-token";

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = "revoked@test.com",
                    Username = "revoked-user",
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                var refreshToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiresAt = DateTime.UtcNow.AddDays(5),
                    IsRevoked = true
                };

                dbContext.Set<RefreshToken>().Add(refreshToken);

                await dbContext.SaveChangesAsync();
            });

            var dto = new RefreshTokenDto
            {
                RefreshToken = refreshTokenValue
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsExpired_ShouldReturnUnauthorized()
        {
            // Arrange
            const string refreshTokenValue = "expired-refresh-token";

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = "expired@test.com",
                    Username = "expired-user",
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                var refreshToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
                    IsRevoked = false
                };

                dbContext.Set<RefreshToken>().Add(refreshToken);

                await dbContext.SaveChangesAsync();
            });

            var dto = new RefreshTokenDto
            {
                RefreshToken = refreshTokenValue
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken = await dbContext.Set<RefreshToken>()
                    .SingleAsync(r => r.Token == refreshTokenValue);

                refreshToken.IsRevoked.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Refresh_WhenUserIsInactive_ShouldReturnUnauthorized()
        {
            // Arrange
            const string refreshTokenValue = "inactive-user-refresh-token";

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = "inactive-refresh@test.com",
                    Username = "inactive-refresh-user",
                    PasswordHash = "not-used-here",
                    IsActive = false
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                var refreshToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiresAt = DateTime.UtcNow.AddDays(5),
                    IsRevoked = false
                };

                dbContext.Set<RefreshToken>().Add(refreshToken);

                await dbContext.SaveChangesAsync();
            });

            var dto = new RefreshTokenDto
            {
                RefreshToken = refreshTokenValue
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken = await dbContext.Set<RefreshToken>()
                    .SingleAsync(r => r.Token == refreshTokenValue);

                refreshToken.IsRevoked.Should().BeFalse();
            });
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsValid_ShouldReturnOkAndRotateRefreshToken()
        {
            // Arrange
            const string oldRefreshTokenValue = "old-valid-refresh-token";

            Guid userId = Guid.Empty;
            Guid oldRefreshTokenId = Guid.Empty;

            await ExecuteDbContextAsync(async dbContext =>
            {
                var user = new User
                {
                    Email = "refresh-success@test.com",
                    Username = "refresh-success-user",
                    PasswordHash = "not-used-here",
                    IsActive = true
                };

                dbContext.Set<User>().Add(user);

                await dbContext.SaveChangesAsync();

                userId = user.Id;

                var refreshToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = oldRefreshTokenValue,
                    ExpiresAt = DateTime.UtcNow.AddDays(5),
                    IsRevoked = false
                };

                dbContext.Set<RefreshToken>().Add(refreshToken);

                await dbContext.SaveChangesAsync();

                oldRefreshTokenId = refreshToken.Id;
            });

            var dto = new RefreshTokenDto
            {
                RefreshToken = oldRefreshTokenValue
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            result.Should().NotBeNull();
            result!.AccessToken.Should().NotBeNullOrWhiteSpace();
            result.RefreshToken.Should().NotBeNullOrWhiteSpace();
            result.RefreshToken.Should().NotBe(oldRefreshTokenValue);
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var oldRefreshToken = await dbContext.Set<RefreshToken>()
                    .SingleAsync(r => r.Id == oldRefreshTokenId);

                oldRefreshToken.IsRevoked.Should().BeTrue();

                var newRefreshToken = await dbContext.Set<RefreshToken>()
                    .SingleOrDefaultAsync(r => r.Token == result.RefreshToken);

                newRefreshToken.Should().NotBeNull();
                newRefreshToken!.UserId.Should().Be(userId);
                newRefreshToken.IsRevoked.Should().BeFalse();
                newRefreshToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            });
        }

        [Fact]
        public async Task Logout_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "some-refresh-token"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/logout", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WhenRefreshTokenDoesNotExist_ShouldReturnUnauthorized()
        {
            // Arrange
            await AuthenticateAsync();

            var dto = new RefreshTokenDto
            {
                RefreshToken = "unknown-refresh-token"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/logout", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WhenRefreshTokenIsAlreadyRevoked_ShouldReturnUnauthorized()
        {
            // Arrange
            var authResponse = await AuthenticateAsync();

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken = await dbContext.Set<RefreshToken>()
                    .SingleAsync(r => r.Token == authResponse.RefreshToken);

                refreshToken.IsRevoked = true;

                await dbContext.SaveChangesAsync();
            });

            var dto = new RefreshTokenDto
            {
                RefreshToken = authResponse.RefreshToken
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/logout", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WhenRefreshTokenIsValid_ShouldReturnNoContentAndRevokeToken()
        {
            // Arrange
            var authResponse = await AuthenticateAsync();

            var dto = new RefreshTokenDto
            {
                RefreshToken = authResponse.RefreshToken
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/logout", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async dbContext =>
            {
                var refreshToken = await dbContext.Set<RefreshToken>()
                    .SingleAsync(r => r.Token == authResponse.RefreshToken);

                refreshToken.IsRevoked.Should().BeTrue();
            });
        }
    }
}