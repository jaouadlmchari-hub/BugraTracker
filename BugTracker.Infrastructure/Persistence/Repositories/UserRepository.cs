using BugTracker.Application.DTOs.Users;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using BugTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(BugTrackerDbContext context): base(context)
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

    public async Task<bool> IsEmailUniqueAsync(string email,Guid? excludeUserId = null)
    {
        return !await _dbSet
                     .AnyAsync(u => u.Email == email &&
                     (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
                    .Where(u => u.IsActive)
                    .ToListAsync();
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPaginatedAsync(
     UserFilterDto filter)
    {
        IQueryable<User> query = _dbSet;

    
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.Email.Contains(search) ||
                u.FullName.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u =>
                u.IsActive == filter.IsActive.Value);
        }

       
        var totalCount = await query.CountAsync();

       
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}