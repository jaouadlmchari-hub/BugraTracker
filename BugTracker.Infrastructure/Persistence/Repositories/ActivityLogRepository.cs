using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class ActivityLogRepository : Repository<ActivityLog> , IActivityLogRepository
    {
        public ActivityLogRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<IEnumerable<ActivityLog>> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                         .Where(a => a.Issue.ProjectId == projectId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetByIssueIdAsync(Guid issueId)
        {
            return await _dbSet
                         .Where(a => a.IssueId == issueId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetByUserIdAsync(Guid projectId, Guid userId)
        {
            return await _dbSet
                        .Where(a => a.Issue.ProjectId == projectId &&
                                    a.UserId == userId)
                         .ToListAsync();
        }
    }
}
