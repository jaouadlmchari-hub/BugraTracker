using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class ProjectMemberRepository : Repository<ProjectMember> , IProjectMemberRepository
    {
        public ProjectMemberRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId)
        {
            return await _dbSet
                         .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        }

        public async Task<IEnumerable<ProjectMember>> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                      .Include(pm => pm.User)
                      .Where(pm => pm.ProjectId == projectId)
                      .ToListAsync();
        }


        public async Task<IEnumerable<ProjectMember>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                         .Where(pm => pm.UserId == userId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<ProjectMember>> GetByRoleAsync(Guid projectId, ProjectRole role)
        {
            return await _dbSet
                         .Where(pm => pm.ProjectId == projectId && pm.Role == role)
                         .ToListAsync();
        }

        public async Task<bool> IsMemberAsync(Guid projectId , Guid userId)
        {
            return await _dbSet
                         .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId==userId);
        }

        public async Task<int> CountManagersAsync(Guid projectId)
        {
            return await _dbSet
                .CountAsync(m => m.ProjectId == projectId && m.Role == ProjectRole.Manager);
        }

    }
}
