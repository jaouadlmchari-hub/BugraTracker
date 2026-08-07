using BugTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{
    public interface IEpicRepository : IRepository<Epic>
    {
        Task<IEnumerable<Epic>> GetByProjectIdAsync(Guid projectId);

        Task<IEnumerable<Epic>> GetActiveEpicsAsync(Guid projectId);

        Task<Epic?> GetByTitleAsync(Guid projectId, string title);
    }
}
