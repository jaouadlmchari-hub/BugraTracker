using BugTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{

    public interface IActivityLogRepository : IRepository<ActivityLog>
    {
        Task<IEnumerable<ActivityLog>> GetByUserIdAsync(Guid projectId, Guid userId);

        Task<IEnumerable<ActivityLog>> GetByProjectIdAsync(Guid projectId);

        Task<IEnumerable<ActivityLog>> GetByIssueIdAsync(Guid issueId);
    }
}
