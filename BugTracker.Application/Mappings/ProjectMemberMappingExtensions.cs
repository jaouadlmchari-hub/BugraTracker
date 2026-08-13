using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings;

public static class ProjectMemberMappingExtensions
{
    public static ProjectMemberDto ToDto(this ProjectMember member)
    {
        return new ProjectMemberDto
        {
            UserId = member.UserId,
            Username = member.User.Username,
            FullName = member.User.FullName,
            Role = member.Role
        };
    }
}