using BugTracker.Application.Configuration;
using BugTracker.Application.DTOs.Auth;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
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

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IRefreshTokenGenerator refreshTokenGenerator,
            IOptions<AuthenticationSettings> authenticationOptions)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenGenerator = refreshTokenGenerator;
            _authenticationSettings = authenticationOptions.Value;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // 1. Chercher l'utilisateur
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);

            // 2. Email inexistant
            if (user == null)
            {
                throw new UnauthorizedException(
                    "Email ou mot de passe incorrect.");
            }

            // 3. Compte désactivé
            if (!user.IsActive)
            {
                throw new UnauthorizedException(
                    "Ce compte est désactivé.");
            }

            // 4. Vérifier si le compte est actuellement verrouillé
            if (user.LockoutUntil.HasValue &&
                user.LockoutUntil.Value > DateTime.UtcNow)
            {
                throw new UnauthorizedException(
                    "Ce compte est temporairement verrouillé.");
            }

            // 5. Vérifier le mot de passe
            var passwordIsValid =
                _passwordHasher.Verify(
                    dto.Password,
                    user.PasswordHash);

            if (!passwordIsValid)
            {
                user.FailedLoginAttempts++;

                // 6. Verrouiller le compte après trop d'échecs
                if (user.FailedLoginAttempts >= _authenticationSettings.MaxFailedLoginAttempts)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(
                        _authenticationSettings.LockoutDurationMinutes);
                }

                await _unitOfWork.SaveChangesAsync();

                throw new UnauthorizedException(
                    "Email ou mot de passe incorrect.");
            }

            // 7. Login réussi : réinitialiser le lockout
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;

            // 8. Générer l'Access Token
            var accessToken =
                _tokenService.GenerateAccessToken(user);

            // 9. Générer le Refresh Token
            var refreshTokenResult =
                _refreshTokenGenerator.Generate();

            // 10. Créer l'entité RefreshToken
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt
            };

            // 11. Ajouter le Refresh Token
            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);

            // 12. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 13. Retourner les tokens
            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshTokenResult.Token,
                ExpiresAt = accessToken.ExpiresAt
            };
        }

        public async Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto)
        {
            // 1. Chercher le Refresh Token en base
            var existingRefreshToken = await _unitOfWork.RefreshTokens
                .GetByTokenAsync(dto.RefreshToken);

            // 2. Vérifier qu'il existe
            if (existingRefreshToken == null)
            {
                throw new UnauthorizedException(
                    "Refresh token invalide.");
            }

            // 3. Vérifier qu'il n'est pas déjà révoqué
            if (existingRefreshToken.IsRevoked)
            {
                throw new UnauthorizedException(
                    "Refresh token révoqué.");
            }

            // 4. Vérifier qu'il n'est pas expiré
            if (existingRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedException(
                    "Refresh token expiré.");
            }

            // 5. Récupérer l'utilisateur lié au token
            var user = await _unitOfWork.Users
                .GetByIdAsync(existingRefreshToken.UserId);

            if (user == null)
            {
                throw new UnauthorizedException(
                    "Utilisateur introuvable.");
            }

            // 6. Vérifier que le compte est toujours actif
            if (!user.IsActive)
            {
                throw new UnauthorizedException(
                    "Ce compte est désactivé.");
            }

            // 7. Révoquer l'ancien Refresh Token
            await _unitOfWork.RefreshTokens
                .RevokeAsync(existingRefreshToken.Id);

            // 8. Générer un nouvel Access Token
            var accessToken =
                _tokenService.GenerateAccessToken(user);

            // 9. Générer un nouveau Refresh Token
            var newRefreshTokenResult =
                _refreshTokenGenerator.Generate();

            // 10. Créer le nouveau Refresh Token
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenResult.Token,
                ExpiresAt = newRefreshTokenResult.ExpiresAt
            };

            // 11. Ajouter le nouveau Refresh Token
            await _unitOfWork.RefreshTokens
                .AddAsync(newRefreshToken);

            // 12. Sauvegarder la rotation
            await _unitOfWork.SaveChangesAsync();

            // 13. Retourner les nouveaux tokens
            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshTokenResult.Token,
                ExpiresAt = accessToken.ExpiresAt
            };
        }

        public async Task LogoutAsync(RefreshTokenDto dto)
        {
            // 1. Chercher le Refresh Token
            var refreshToken = await _unitOfWork.RefreshTokens
                .GetByTokenAsync(dto.RefreshToken);

            // 2. Vérifier qu'il existe
            if (refreshToken == null)
            {
                throw new UnauthorizedException(
                    "Refresh token invalide.");
            }

            // 3. Vérifier qu'il n'est pas déjà révoqué
            if (refreshToken.IsRevoked)
            {
                throw new UnauthorizedException(
                    "Refresh token déjà révoqué.");
            }

            // 4. Révoquer le Refresh Token
            await _unitOfWork.RefreshTokens
                .RevokeAsync(refreshToken.Id);

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}