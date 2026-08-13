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
        Task<IEnumerable<Issue>> GetByProjectIdAsync(Guid projectId);

        Task<Issue?> GetByIdWithDetailsAsync(Guid issueId);

        Task<IEnumerable<Issue>> GetBySprintIdAsync(Guid sprintId);

        Task<IEnumerable<Issue>> GetByAssigneeIdAsync(Guid assigneeId);

        Task<IEnumerable<Issue>> GetByReporterIdAsync(Guid reporterId);

        Task<IEnumerable<Issue>> GetByStatusAsync(Guid projectId, IssueStatus status);

        Task<IEnumerable<Issue>> GetByPriorityAsync(Guid projectId, Priority priority);

        Task<IEnumerable<Issue>> GetByProjectAndAssigneeAsync(Guid projectId, Guid userId);

        Task<IEnumerable<Issue>> GetUnfinishedBySprintIdAsync(Guid sprintId);

    }
}
