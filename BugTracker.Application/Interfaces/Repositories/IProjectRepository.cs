using BugTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project?> GetByKeyAsync(string key);

        Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId);

        Task<IEnumerable<Project>> GetActiveProjectsAsync();

        Task<bool> IsKeyUniqueAsync(string key);
    }
}
