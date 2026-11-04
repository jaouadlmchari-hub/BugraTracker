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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.");
        }

        services.AddHealthChecks()
            // Liveness : l'API tourne
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })

            // Readiness : SQL Server accessible
            .AddSqlServer(

                connectionString: connectionString ?? string.Empty,
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" });

        return services;
    }

    public static IApplicationBuilder UseBugTrackerHealthChecks(this IApplicationBuilder app)
    {
        // /health/live  → process up
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // /health/ready → dépendances OK (SQL, etc.)
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // /health → tout
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}