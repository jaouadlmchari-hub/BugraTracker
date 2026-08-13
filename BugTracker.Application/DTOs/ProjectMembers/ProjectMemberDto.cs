using BugTracker.Domain.Enums;


namespace BugTracker.Application.DTOs.ProjectMembers
{
    public class ProjectMemberDto
    {
        public Guid UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public ProjectRole Role { get; set; }
    }
}
