using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class IssueRepository : Repository<Issue> , IIssueRepository
    {
        public IssueRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<IEnumerable<Issue>> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                         .Where(i => i.ProjectId == projectId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetBySrpintIdAsync(Guid sprintId)
        {
            return await _dbSet
                         .Where(i => i.SprintId == sprintId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByAssigneeIdAsync(Guid assigneeId)
        {
            return await _dbSet
                         .Where(i => i.AssigneeId == assigneeId)
                         .ToListAsync();
        }


        public async Task<IEnumerable<Issue>> GetByReporterIdAsync(Guid reporterId)
        {
            return await _dbSet
                         .Where(i => i.ReporterId == reporterId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByStatusAsync(Guid projectId,IssueStatus status)
        {
            return await _dbSet
                .Where(i => i.ProjectId == projectId &&
                            i.Status == status)
                .ToListAsync();
        }


        public async Task<IEnumerable<Issue>> GetByPriorityAsync(Guid projectId, Priority priority)
        {
            return await _dbSet
                .Where(i => i.ProjectId == projectId &&
                            i.Priority == priority)
                .ToListAsync();
        }
    }
}
