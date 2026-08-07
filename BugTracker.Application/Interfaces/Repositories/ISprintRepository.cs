using BugTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{
    public interface ISprintRepository : IRepository<Sprint>
    {
        Task<IEnumerable<Sprint>> GetByProjectIdAsync(Guid projectId);

        Task<IEnumerable<Sprint>> GetActiveSprintsAsync(Guid projectId);

        Task<Sprint?> GetByNameAsync(Guid projectId, string name);
    }
}
