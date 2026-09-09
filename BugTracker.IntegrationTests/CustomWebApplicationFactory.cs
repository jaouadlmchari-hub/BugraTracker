using BugTracker.API;
using BugTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respawn.Graph;
using Respawn;

namespace BugTracker.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private Respawner _respawner = null!;
        private string _connectionString = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<BugTrackerDbContext>();

            dbContext.Database.Migrate();

            _connectionString = dbContext.Database.GetConnectionString()!;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            _respawner = Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,

                SchemasToInclude = new[]
                {
                    "dbo"
                },

                TablesToIgnore = new Table[]
                {
                    "__EFMigrationsHistory"
                }
            }).GetAwaiter().GetResult();

            return host;
        }

        public async Task ResetDatabaseAsync()
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await _respawner.ResetAsync(connection);
        }
    }
}