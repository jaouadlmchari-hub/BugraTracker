using BugTracker.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace BugTracker.Infrastructure.Extensions;

public static class DatabaseInitializerExtensions
{
    public static async Task UseDatabaseSeederAsync(
        this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var seeder = scope.ServiceProvider
            .GetRequiredService<DatabaseSeeder>();

        await seeder.SeedAsync();
    }
}