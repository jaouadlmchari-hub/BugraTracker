using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Services
{
    public interface  IProjectMemberService
    {
        Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId);

        Task<ProjectMemberDto> AddMemberAsync(Guid projectId,AddProjectMemberDto dto);

        Task ChangeRoleAsync( Guid projectId, Guid userId, ProjectRole newRole);

        Task RemoveMemberAsync(Guid projectId, Guid userId);
    }
}
