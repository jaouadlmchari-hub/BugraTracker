using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        _logger.LogDebug(
            "Looking up refresh token.");

        var refreshToken = await _unitOfWork.RefreshTokens
            .GetByTokenAsync(token);

        if (refreshToken == null)
        {
            _logger.LogWarning(
                "Refresh token lookup failed: token not found.");
        }
        else
        {
            _logger.LogDebug(
                "Refresh token found. TokenId: {TokenId}, UserId: {UserId}",
                refreshToken.Id,
                refreshToken.UserId);
        }

        return refreshToken;
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        _logger.LogDebug(
            "Retrieving refresh tokens for UserId: {UserId}",
            userId);

        var tokens = await _unitOfWork.RefreshTokens
            .GetByUserIdAsync(userId);

        var result = tokens.ToList();

        _logger.LogInformation(
            "Retrieved {Count} refresh tokens for UserId: {UserId}",
            result.Count,
            userId);

        return result;
    }

    public async Task RevokeAsync(Guid tokenId)
    {
        _logger.LogInformation(
            "Revoking refresh token. TokenId: {TokenId}",
            tokenId);

        await _unitOfWork.RefreshTokens
            .RevokeAsync(tokenId);

        _logger.LogInformation(
            "Refresh token revoked successfully. TokenId: {TokenId}",
            tokenId);
    }
}