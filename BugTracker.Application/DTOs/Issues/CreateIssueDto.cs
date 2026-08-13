using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Issues;

public class CreateIssueDto
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IssueType Type { get; set; }

    public Priority? Priority { get; set; }

    public int? StoryPoints { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid? EpicId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid? AssigneeId { get; set; }
}