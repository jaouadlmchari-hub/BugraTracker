using BugTracker.Domain.Entities;

public interface IRefreshTokenService
{
    Task<RefreshToken?> GetByTokenAsync(string token);

    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);

    Task RevokeAsync(Guid tokenId);
}