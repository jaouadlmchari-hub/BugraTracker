using BugTracker.Application.Interfaces;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _unitOfWork.RefreshTokens
            .GetByTokenAsync(token);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _unitOfWork.RefreshTokens
            .GetByUserIdAsync(userId);
    }

    public async Task RevokeAsync(Guid tokenId)
    {
        await _unitOfWork.RefreshTokens
            .RevokeAsync(tokenId);
    }
}