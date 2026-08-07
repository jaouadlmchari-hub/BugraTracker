using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(BugTrackerDbContext context) : base(context)
        {
        }

        public async Task<Project?> GetByKeyAsync(string key)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Key == key);
        }

        public async Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _dbSet
                .Where(p => p.OwnerId == ownerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetActiveProjectsAsync()
        {
            return await _dbSet
                .Where(p => p.Status == ProjectStatus.Active)
                .ToListAsync();
        }

        public async Task<bool> IsKeyUniqueAsync(string key)
        {
            return !await _dbSet
                .AnyAsync(p => p.Key == key);
        }
    }
}
