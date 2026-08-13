using Amazon.S3;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Infrastructure.Configurations;
using BugTracker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BugTracker.Infrastructure.Extensions;

public static class S3Configuration
{
    public static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Charger la section S3 dans S3Settings
        services.Configure<S3Settings>(
            configuration.GetSection("S3"));

        // 2. Enregistrer le client S3
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var settings = sp
                .GetRequiredService<IOptions<S3Settings>>()
                .Value;

            var s3Config = new AmazonS3Config
            {
                ServiceURL = settings.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = settings.Region
            };

            return new AmazonS3Client(
                settings.AccessKey,
                settings.SecretKey,
                s3Config);
        });

        // 3. Enregistrer notre abstraction de stockage
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        return services;
    }
}