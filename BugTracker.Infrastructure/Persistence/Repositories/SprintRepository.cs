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
    public class SprintRepository : Repository<Sprint> , ISprintRepository
    {
        public SprintRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<IEnumerable<Sprint>> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                         .Where(s => s.ProjectId == projectId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<Sprint>> GetActiveSprintsAsync(Guid projectId)
        {
            return await _dbSet
                         .Where(s => s.ProjectId == projectId &&
                                     s.Status == SprintStatus.Active)
                         .ToListAsync();
        }
        public async Task<Sprint?> GetByNameAsync(Guid projectId , string name)
        {
            return await _dbSet
                         .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Name == name);
        }
    }
}
