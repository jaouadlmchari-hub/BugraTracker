

using System.ComponentModel.DataAnnotations;

namespace BugTracker.Application.DTOs.Issues
{
    public class ReorderIssueItemDto
    {
        public Guid IssueId { get; set; }

        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }
    }
}
