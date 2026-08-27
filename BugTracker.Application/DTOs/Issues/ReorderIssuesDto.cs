using System.ComponentModel.DataAnnotations;


namespace BugTracker.Application.DTOs.Issues
{
    public class ReorderIssuesDto
    {
        [MinLength(1)]
        public List<ReorderIssueItemDto> Items { get; set; } = new();
    }
}
