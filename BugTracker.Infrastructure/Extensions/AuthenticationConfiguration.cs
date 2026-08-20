using BugTracker.Application.Interfaces.Services;
using BugTracker.Infrastructure.Configurations;
using BugTracker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BugTracker.Infrastructure.Extensions;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Charger la section "Jwt" dans JwtSettings
        services.Configure<JwtSettings>(
            configuration.GetSection("Jwt"));

        // 1. Charger la section "RefreshToken" dans RefreshTokenSettings
        services.Configure<RefreshTokenSettings>(
            configuration.GetSection("RefreshToken"));

        // 3. Enregistrer l'implémentation de ITokenService
        services.AddScoped<ITokenService, JwtTokenService>();

        // 4. Enregistrer l'implémentation de IRefreshTokenGenerator
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();



        return services;
    }
}