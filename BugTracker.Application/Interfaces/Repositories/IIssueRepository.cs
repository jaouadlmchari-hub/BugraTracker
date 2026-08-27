using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{
    public  interface IIssueRepository : IRepository<Issue>
    {
        Task<Issue?> GetByIdWithDetailsAsync(Guid issueId);

        Task<(IEnumerable<Issue> Items, int TotalCount)> GetPaginatedAsync(Guid projectId,IssueFilterDto filter);

        Task<IEnumerable<Issue>> GetByProjectAndAssigneeAsync(Guid projectId, Guid userId);

        Task<IEnumerable<Issue>> GetUnfinishedBySprintIdAsync(Guid sprintId);

        Task<IEnumerable<Issue>> GetByIdsAsync(IEnumerable<Guid> issueIds);

    }
}
