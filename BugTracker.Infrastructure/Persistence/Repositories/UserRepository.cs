using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using BugTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(BugTrackerDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        return !await _dbSet
            .AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .ToListAsync();
    }
}