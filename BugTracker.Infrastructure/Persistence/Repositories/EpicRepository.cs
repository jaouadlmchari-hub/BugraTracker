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
    public class EpicRepository : Repository<Epic> , IEpicRepository
    {
        public EpicRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<IEnumerable<Epic>> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                         .Where(e => e.ProjectId == projectId)
                         .ToListAsync();
        }
        public async Task<IEnumerable<Epic>> GetActiveEpicsAsync(Guid projectId)
        {
            return await _dbSet
                .Where(e => e.ProjectId == projectId &&
                            e.Status == EpicStatus.Active)
                .ToListAsync();
        }

        public async Task<Epic?> GetByTitleAsync(Guid projectId , string title)
        {
            return await _dbSet
                         .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.Title == title);
        }
    }
}
