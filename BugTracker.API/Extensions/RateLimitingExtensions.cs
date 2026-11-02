using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace BugTracker.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddBugTrackerRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Trop de requêtes. Veuillez réessayer plus tard.",
                    retryAfterSeconds = 60
                }, cancellationToken);
            };

            // -------------------------------------------------
            // 1. GlobalLimiter : 100 req/min par IP (automatique)
            // -------------------------------------------------
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

            // -------------------------------------------------
            // 2. Politique "auth" : anti brute-force (5/min) 
            // -------------------------------------------------
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            // -------------------------------------------------
            // 3. Politique "upload" : 10 uploads / min 
            // -------------------------------------------------

            options.AddFixedWindowLimiter("upload", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
        });

        return services;
    }
}