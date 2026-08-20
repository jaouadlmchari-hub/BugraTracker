using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Models.Auth;
using BugTracker.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace BugTracker.Infrastructure.Services;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenGenerator(IOptions<RefreshTokenSettings> options)
    {
        _settings = options.Value;
    }

    public RefreshTokenResult Generate()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(64);

        var token =
            Convert.ToBase64String(randomBytes);

        var expiresAt =
            DateTime.UtcNow.AddDays(_settings.ExpirationDays);

        return new RefreshTokenResult(
            token,
            expiresAt);
    }
}