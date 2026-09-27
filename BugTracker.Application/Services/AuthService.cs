using BugTracker.Application.Configuration;
using BugTracker.Application.DTOs.Auth;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BugTracker.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IRefreshTokenGenerator refreshTokenGenerator,
            IOptions<AuthenticationSettings> authenticationOptions,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenGenerator = refreshTokenGenerator;
            _authenticationSettings = authenticationOptions.Value;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation(
                "Login attempt for email {Email}",
                dto.Email);

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning(
                    "Login failed: user not found for email {Email}",
                    dto.Email);

                throw new UnauthorizedException(
                    "Email ou mot de passe incorrect.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login failed: inactive account for user {UserId}",
                    user.Id);

                throw new UnauthorizedException(
                    "Ce compte est désactivé.");
            }

            if (user.LockoutUntil.HasValue &&
                user.LockoutUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Login failed: account {UserId} is locked until {LockoutUntil}",
                    user.Id,
                    user.LockoutUntil);

                throw new UnauthorizedException(
                    "Ce compte est temporairement verrouillé.");
            }

            var passwordIsValid =
                _passwordHasher.Verify(
                    dto.Password,
                    user.PasswordHash);

            if (!passwordIsValid)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >=
                    _authenticationSettings.MaxFailedLoginAttempts)
                {
                    user.LockoutUntil =
                        DateTime.UtcNow.AddMinutes(
                            _authenticationSettings.LockoutDurationMinutes);

                    _logger.LogWarning(
                        "User {UserId} locked after {FailedAttempts} failed login attempts until {LockoutUntil}",
                        user.Id,
                        user.FailedLoginAttempts,
                        user.LockoutUntil);
                }
                else
                {
                    _logger.LogWarning(
                        "Login failed for user {UserId}. Failed attempts: {FailedAttempts}",
                        user.Id,
                        user.FailedLoginAttempts);
                }

                await _unitOfWork.SaveChangesAsync();

                throw new UnauthorizedException(
                    "Email ou mot de passe incorrect.");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;

            var accessToken =
                _tokenService.GenerateAccessToken(user);

            var refreshTokenResult =
                _refreshTokenGenerator.Generate();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} logged in successfully",
                user.Id);

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshTokenResult.Token,
                ExpiresAt = accessToken.ExpiresAt
            };
        }

        public async Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto)
        {
            _logger.LogInformation(
                "Refresh token request received");

            var existingRefreshToken =
                await _unitOfWork.RefreshTokens
                    .GetByTokenAsync(dto.RefreshToken);

            if (existingRefreshToken == null)
            {
                _logger.LogWarning(
                    "Refresh failed: token not found");

                throw new UnauthorizedException(
                    "Refresh token invalide.");
            }

            if (existingRefreshToken.IsRevoked)
            {
                _logger.LogWarning(
                    "Refresh failed: token {RefreshTokenId} is already revoked",
                    existingRefreshToken.Id);

                throw new UnauthorizedException(
                    "Refresh token révoqué.");
            }

            if (existingRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Refresh failed: token {RefreshTokenId} is expired",
                    existingRefreshToken.Id);

                throw new UnauthorizedException(
                    "Refresh token expiré.");
            }

            var user = await _unitOfWork.Users
                .GetByIdAsync(existingRefreshToken.UserId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Refresh failed: user {UserId} not found",
                    existingRefreshToken.UserId);

                throw new UnauthorizedException(
                    "Utilisateur introuvable.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Refresh failed: inactive user {UserId}",
                    user.Id);

                throw new UnauthorizedException(
                    "Ce compte est désactivé.");
            }

            await _unitOfWork.RefreshTokens
                .RevokeAsync(existingRefreshToken.Id);

            var accessToken =
                _tokenService.GenerateAccessToken(user);

            var newRefreshTokenResult =
                _refreshTokenGenerator.Generate();

            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenResult.Token,
                ExpiresAt = newRefreshTokenResult.ExpiresAt
            };

            await _unitOfWork.RefreshTokens
                .AddAsync(newRefreshToken);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Refresh token rotated successfully for user {UserId}",
                user.Id);

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshTokenResult.Token,
                ExpiresAt = accessToken.ExpiresAt
            };
        }

        public async Task LogoutAsync(RefreshTokenDto dto)
        {
            _logger.LogInformation(
                "Logout request received");

            var refreshToken =
                await _unitOfWork.RefreshTokens
                    .GetByTokenAsync(dto.RefreshToken);

            if (refreshToken == null)
            {
                _logger.LogWarning(
                    "Logout failed: refresh token not found");

                throw new UnauthorizedException(
                    "Refresh token invalide.");
            }

            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning(
                    "Logout failed: refresh token {RefreshTokenId} already revoked",
                    refreshToken.Id);

                throw new UnauthorizedException(
                    "Refresh token déjà révoqué.");
            }

            await _unitOfWork.RefreshTokens
                .RevokeAsync(refreshToken.Id);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} logged out successfully",
                refreshToken.UserId);
        }
    }
}