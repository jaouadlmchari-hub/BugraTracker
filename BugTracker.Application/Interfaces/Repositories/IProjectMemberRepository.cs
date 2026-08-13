using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{
    public interface IProjectMemberRepository : IRepository<ProjectMember>
    {
        Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId);

        Task<IEnumerable<ProjectMember>> GetByProjectIdAsync(Guid projectId);

        Task<IEnumerable<ProjectMember>> GetByUserIdAsync(Guid userId);

        Task<IEnumerable<ProjectMember>> GetByRoleAsync(Guid projectId, ProjectRole role);

        Task<bool> IsMemberAsync(Guid projectId, Guid userId);

        Task<int> CountManagersAsync(Guid projectId);
    }
}
