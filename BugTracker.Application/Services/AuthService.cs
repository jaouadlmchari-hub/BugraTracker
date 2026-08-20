using BugTracker.Application.DTOs.Auth;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IRefreshTokenGenerator refreshTokenGenerator)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenGenerator = refreshTokenGenerator;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // 1. Chercher l'utilisateur
            var user = await _unitOfWork.Users
                .GetByEmailAsync(dto.Email);

            // 2. Vérifier les credentials
            if (user == null ||
                !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedException(
                    "Email ou mot de passe incorrect.");
            }

            // 3. Générer l'Access Token
            var accessToken =
                _tokenService.GenerateAccessToken(user);

            // 4. Générer le Refresh Token
            var refreshTokenResult =
                _refreshTokenGenerator.Generate();

            // 5. Créer l'entité RefreshToken
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt
            };

            // 6. Ajouter en base
            await _unitOfWork.RefreshTokens
                .AddAsync(refreshToken);

            // 7. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 8. Retourner les tokens
            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshTokenResult.Token,
                ExpiresAt = accessToken.ExpiresAt
            };
        }
    }
}