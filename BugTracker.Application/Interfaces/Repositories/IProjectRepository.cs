using BugTracker.Application.DTOs.Projects;
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

        Task<Project?> GetByIdWithMembersAsync(Guid projectId);
        Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId);

        Task<IEnumerable<Project>> GetActiveProjectsAsync();

        Task<bool> ExistsAsync(Guid projectId);

        Task<(IEnumerable<Project> Items, int TotalCount)> GetPaginatedAsync(
           ProjectFilterDto filter,Guid userId, bool isAdmin);

        Task<bool> IsKeyUniqueAsync(string key);
    }
}
