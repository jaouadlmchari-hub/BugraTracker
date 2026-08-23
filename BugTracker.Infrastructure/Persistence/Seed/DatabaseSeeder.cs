using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Infrastructure.Persistence.Seed
{
    public class DatabaseSeeder
    {
        private readonly BugTrackerDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public DatabaseSeeder(
            BugTrackerDbContext dbContext,
            IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync()
        {
            await _dbContext.Database.MigrateAsync();

            var adminEmail = "admin@bugtracker.com";
            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Email == adminEmail);

            if (!userExists)
            {
                var adminUser = new User
                {
                    Email = adminEmail,
                    Username = "admin",
                    FullName = "System Administrator",
                    PasswordHash = _passwordHasher.Hash("Admin123!"),
                    SystemRole = SystemRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}