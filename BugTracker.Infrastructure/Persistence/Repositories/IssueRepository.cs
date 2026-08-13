using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class IssueRepository : Repository<Issue> , IIssueRepository
    {
        public IssueRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<Issue?> GetByIdWithDetailsAsync(Guid issueId)
        {
            return await _dbSet
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .FirstOrDefaultAsync(i => i.Id == issueId);
        }

        public async Task<IEnumerable<Issue>> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                .Where(i => i.ProjectId == projectId)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetBySprintIdAsync(Guid sprintId)
        {
            return await _dbSet
                .Where(i => i.SprintId == sprintId)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByAssigneeIdAsync(Guid assigneeId)
        {
            return await _dbSet
                .Where(i => i.AssigneeId == assigneeId)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByReporterIdAsync(Guid reporterId)
        {
            return await _dbSet
                .Where(i => i.ReporterId == reporterId)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByStatusAsync(Guid projectId,IssueStatus status)
        {
            return await _dbSet
                .Where(i =>
                    i.ProjectId == projectId &&
                    i.Status == status)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByPriorityAsync(Guid projectId,Priority priority)
        {
            return await _dbSet
                .Where(i =>
                    i.ProjectId == projectId &&
                    i.Priority == priority)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetByProjectAndAssigneeAsync(Guid projectId,Guid userId)
        {
            return await _dbSet
                .Where(i =>
                    i.ProjectId == projectId &&
                    i.AssigneeId == userId)
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Issue>> GetUnfinishedBySprintIdAsync(Guid sprintId)
        {
            return await _dbSet
                .Where(i =>
                    i.SprintId == sprintId &&
                    (i.Status == IssueStatus.Todo ||
                     i.Status == IssueStatus.InProgress))
                .ToListAsync();
        }
    }
}
