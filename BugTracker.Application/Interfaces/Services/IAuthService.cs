using BugTracker.Application.DTOs.Auth;

namespace BugTracker.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto);

    Task LogoutAsync(RefreshTokenDto dto);
}