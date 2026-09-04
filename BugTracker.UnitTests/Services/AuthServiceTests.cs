using BugTracker.Application.Configuration;
using BugTracker.Application.DTOs.Auth;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Models.Auth;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BugTracker.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();

            _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object);

            var authenticationOptions = Options.Create(new AuthenticationSettings
            {
                MaxFailedLoginAttempts = 5,
                LockoutDurationMinutes = 15
            });

            _sut = new AuthService(
                _unitOfWorkMock.Object,
                _passwordHasherMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenGeneratorMock.Object,
                authenticationOptions);
        }

        [Fact]
        public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "unknown@test.com",
                Password = "Password123!"
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.LoginAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Email ou mot de passe incorrect.");

            _passwordHasherMock.Verify(
                h => h.Verify(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsInactive_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = false
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = () => _sut.LoginAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Ce compte est désactivé.");

            _passwordHasherMock.Verify(
                h => h.Verify(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsLockedOut_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = true,
                FailedLoginAttempts = 5,
                LockoutUntil = DateTime.UtcNow.AddMinutes(10)
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = () => _sut.LoginAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Ce compte est temporairement verrouillé.");

            _passwordHasherMock.Verify(
                h => h.Verify(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldIncrementFailedLoginAttemptsAndSave()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = "WrongPassword123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = true,
                FailedLoginAttempts = 2,
                LockoutUntil = null
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Verify(dto.Password, user.PasswordHash))
                .Returns(false);

            // Act
            Func<Task> act = () => _sut.LoginAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Email ou mot de passe incorrect.");

            user.FailedLoginAttempts.Should().Be(3);
            user.LockoutUntil.Should().BeNull();

            _passwordHasherMock.Verify(
                h => h.Verify(dto.Password, user.PasswordHash),
                Times.Once);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WhenFailedAttemptsReachLimit_ShouldLockUser()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = "WrongPassword123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = true,
                FailedLoginAttempts = 4,
                LockoutUntil = null
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Verify(dto.Password, user.PasswordHash))
                .Returns(false);

            var beforeLockout = DateTime.UtcNow;

            // Act
            Func<Task> act = () => _sut.LoginAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Email ou mot de passe incorrect.");

            user.FailedLoginAttempts.Should().Be(5);

            user.LockoutUntil.Should().NotBeNull();

            user.LockoutUntil!.Value.Should()
                .BeOnOrAfter(beforeLockout.AddMinutes(15));

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WhenCredentialsAreValid_ShouldResetLockoutGenerateTokensAndSaveRefreshToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            var dto = new LoginDto
            {
                Email = "jaouad@test.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = userId,
                Email = dto.Email,
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = true,
                FailedLoginAttempts = 3,
                LockoutUntil = DateTime.UtcNow.AddMinutes(-5)
            };

            var accessTokenResult = new AccessTokenResult(
                "access-token",
                accessTokenExpiresAt);

            var refreshTokenResult = new RefreshTokenResult(
                "refresh-token",
                refreshTokenExpiresAt);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Verify(dto.Password, user.PasswordHash))
                .Returns(true);

            _tokenServiceMock
                .Setup(t => t.GenerateAccessToken(user))
                .Returns(accessTokenResult);

            _refreshTokenGeneratorMock
                .Setup(r => r.Generate())
                .Returns(refreshTokenResult);

            RefreshToken? createdRefreshToken = null;

            _refreshTokenRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                .Callback<RefreshToken>(token => createdRefreshToken = token)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.LoginAsync(dto);

            // Assert
            user.FailedLoginAttempts.Should().Be(0);
            user.LockoutUntil.Should().BeNull();

            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.ExpiresAt.Should().Be(accessTokenExpiresAt);

            createdRefreshToken.Should().NotBeNull();
            createdRefreshToken!.UserId.Should().Be(userId);
            createdRefreshToken.Token.Should().Be("refresh-token");
            createdRefreshToken.ExpiresAt.Should().Be(refreshTokenExpiresAt);

            _passwordHasherMock.Verify(
                h => h.Verify(dto.Password, user.PasswordHash),
                Times.Once);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(user),
                Times.Once);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Once);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_WhenRefreshTokenDoesNotExist_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "unknown-refresh-token"
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Func<Task> act = () => _sut.RefreshAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Refresh token invalide.");

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WhenRefreshTokenIsRevoked_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "revoked-refresh-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = true
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(refreshToken);

            // Act
            Func<Task> act = () => _sut.RefreshAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Refresh token révoqué.");

            _userRepositoryMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WhenRefreshTokenIsExpired_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "expired-refresh-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
                IsRevoked = false
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(refreshToken);

            // Act
            Func<Task> act = () => _sut.RefreshAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Refresh token expiré.");

            _userRepositoryMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WhenUserDoesNotExist_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var dto = new RefreshTokenDto
            {
                RefreshToken = "valid-refresh-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = false
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(refreshToken);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.RefreshAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Utilisateur introuvable.");

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WhenUserIsInactive_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var dto = new RefreshTokenDto
            {
                RefreshToken = "valid-refresh-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = false
            };

            var user = new User
            {
                Id = userId,
                Email = "jaouad@test.com",
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = false
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(refreshToken);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = () => _sut.RefreshAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Ce compte est désactivé.");

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(It.IsAny<User>()),
                Times.Never);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Never);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WhenRefreshTokenIsValid_ShouldRotateRefreshTokenAndReturnNewTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldRefreshTokenId = Guid.NewGuid();

            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            var dto = new RefreshTokenDto
            {
                RefreshToken = "old-refresh-token"
            };

            var existingRefreshToken = new RefreshToken
            {
                Id = oldRefreshTokenId,
                UserId = userId,
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                IsRevoked = false
            };

            var user = new User
            {
                Id = userId,
                Email = "jaouad@test.com",
                Username = "jaouad",
                PasswordHash = "hashed-password",
                IsActive = true
            };

            var accessTokenResult = new AccessTokenResult(
                "new-access-token",
                accessTokenExpiresAt);

            var newRefreshTokenResult = new RefreshTokenResult(
                "new-refresh-token",
                newRefreshTokenExpiresAt);

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(existingRefreshToken);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _refreshTokenRepositoryMock
                .Setup(r => r.RevokeAsync(oldRefreshTokenId))
                .Returns(Task.CompletedTask);

            _tokenServiceMock
                .Setup(t => t.GenerateAccessToken(user))
                .Returns(accessTokenResult);

            _refreshTokenGeneratorMock
                .Setup(r => r.Generate())
                .Returns(newRefreshTokenResult);

            RefreshToken? createdRefreshToken = null;

            _refreshTokenRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                .Callback<RefreshToken>(token => createdRefreshToken = token)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.RefreshAsync(dto);

            // Assert
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-refresh-token");
            result.ExpiresAt.Should().Be(accessTokenExpiresAt);

            createdRefreshToken.Should().NotBeNull();
            createdRefreshToken!.UserId.Should().Be(userId);
            createdRefreshToken.Token.Should().Be("new-refresh-token");
            createdRefreshToken.ExpiresAt.Should().Be(newRefreshTokenExpiresAt);

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(oldRefreshTokenId),
                Times.Once);

            _tokenServiceMock.Verify(
                t => t.GenerateAccessToken(user),
                Times.Once);

            _refreshTokenGeneratorMock.Verify(
                r => r.Generate(),
                Times.Once);

            _refreshTokenRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<RefreshToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_WhenRefreshTokenDoesNotExist_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "unknown-refresh-token"
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Func<Task> act = () => _sut.LogoutAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Refresh token invalide.");

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_WhenRefreshTokenIsAlreadyRevoked_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var dto = new RefreshTokenDto
            {
                RefreshToken = "revoked-refresh-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = true
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(refreshToken);

            // Act
            Func<Task> act = () => _sut.LogoutAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedException>()
                .WithMessage("Refresh token déjà révoqué.");

            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_WhenRefreshTokenIsValid_ShouldRevokeTokenAndSave()
        {
            // Arrange
            var refreshTokenId = Guid.NewGuid();

            var dto = new RefreshTokenDto
            {
                RefreshToken = "valid-refresh-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = refreshTokenId,
                UserId = Guid.NewGuid(),
                Token = dto.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = false
            };

            _refreshTokenRepositoryMock
                .Setup(r => r.GetByTokenAsync(dto.RefreshToken))
                .ReturnsAsync(refreshToken);

            _refreshTokenRepositoryMock
                .Setup(r => r.RevokeAsync(refreshTokenId))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.LogoutAsync(dto);

            // Assert
            _refreshTokenRepositoryMock.Verify(
                r => r.RevokeAsync(refreshTokenId),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

    }
}