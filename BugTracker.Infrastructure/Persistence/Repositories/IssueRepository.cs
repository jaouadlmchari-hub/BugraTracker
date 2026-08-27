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

        public async Task<(IEnumerable<Issue> Items, int TotalCount)> GetPaginatedAsync(
            Guid projectId, IssueFilterDto filter)
        {
            IQueryable<Issue> query = _dbSet
                .Where(i => i.ProjectId == projectId);

            // Recherche
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(i =>
                    i.Title.Contains(filter.Search) ||
                    (i.Description != null &&
                     i.Description.Contains(filter.Search)));
            }

            // Statut
            if (filter.Status.HasValue)
            {
                query = query.Where(i =>
                    i.Status == filter.Status.Value);
            }

            // Type
            if (filter.Type.HasValue)
            {
                query = query.Where(i =>
                    i.Type == filter.Type.Value);
            }

            // Priorité
            if (filter.Priority.HasValue)
            {
                query = query.Where(i =>
                    i.Priority == filter.Priority.Value);
            }

            // Assignee
            if (filter.AssigneeId.HasValue)
            {
                query = query.Where(i =>
                    i.AssigneeId == filter.AssigneeId.Value);
            }

            // Reporter
            if (filter.ReporterId.HasValue)
            {
                query = query.Where(i =>
                    i.ReporterId == filter.ReporterId.Value);
            }

            // Epic
            if (filter.EpicId.HasValue)
            {
                query = query.Where(i =>
                    i.EpicId == filter.EpicId.Value);
            }

            // Sprint
            if (filter.SprintId.HasValue)
            {
                query = query.Where(i =>
                    i.SprintId == filter.SprintId.Value);
            }

            // Backlog
            if (filter.BacklogOnly == true)
            {
                query = query.Where(i =>
                    i.SprintId == null);
            }

            // Nombre total APRÈS les filtres
            var totalCount = await query.CountAsync();

            // Relations nécessaires pour IssueDto
            var items = await query
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .OrderBy(i => i.DisplayOrder)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
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

        public async Task<IEnumerable<Issue>> GetByIdsAsync(IEnumerable<Guid> issueIds)
        {
            var ids = issueIds.ToList();

            return await _dbSet
                .Where(i => ids.Contains(i.Id))
                .ToListAsync();
        }
    }
}
