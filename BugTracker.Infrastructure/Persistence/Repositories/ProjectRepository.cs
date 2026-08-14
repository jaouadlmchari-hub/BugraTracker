using BugTracker.Application.DTOs.Projects;
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

        public async Task<Project?> GetByIdWithMembersAsync(Guid projectId)
        {
            return await _dbSet
                       .Include(p => p.Members)
                       .FirstOrDefaultAsync(p => p.Id == projectId);
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

        public async Task<bool> ExistsAsync(Guid projectId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId);
        }

        public async Task<(IEnumerable<Project> Items, int TotalCount)> GetPaginatedAsync(
               ProjectFilterDto filter,Guid userId,bool isAdmin)
        {
            IQueryable<Project> query = _dbSet;

            if (!isAdmin)
            {
                query = query.Where(p =>
                    p.Members.Any(m => m.UserId == userId));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(p =>
                    p.Name.Contains(filter.Search) ||
                    p.Key.Contains(filter.Search));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(p =>
                    p.Status == filter.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<bool> IsKeyUniqueAsync(string key)
        {
            return !await _dbSet
                .AnyAsync(p => p.Key == key);
        }
    }
}
