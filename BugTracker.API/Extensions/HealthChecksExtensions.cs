
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BugTracker.API.Extensions;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddBugTrackerHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SQL Server connection
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.");
        }

        // Redis connection
        var redisConnection =
            configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            redisConnection = configuration["Redis:Configuration"];
        }

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            redisConnection = "localhost:6379";
        }

        services.AddHealthChecks()

            // Liveness : API is running
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: new[] { "live" })

            // Readiness : SQL Server is accessible
            .AddSqlServer(
                connectionString: connectionString,
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" })

            // Readiness : Redis is accessible
            .AddRedis(
                redisConnectionString: redisConnection,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" });

        return services;
    }

    public static IApplicationBuilder UseBugTrackerHealthChecks(
        this IApplicationBuilder app)
    {
        // /health/live → API process is running
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // /health/ready → SQL Server + Redis are accessible
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // /health → all health checks
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
