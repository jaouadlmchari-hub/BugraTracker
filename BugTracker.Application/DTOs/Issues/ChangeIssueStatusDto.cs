using BugTracker.Domain.Enums;


namespace BugTracker.Application.DTOs.Issues
{
    public class ChangeIssueStatusDto
    {
        public IssueStatus NewStatus { get; set; }
    }
}
