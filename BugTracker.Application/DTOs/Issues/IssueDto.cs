using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Issues;

public class IssueDto
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? EpicId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid ReporterId { get; set; }
    public string ReporterName { get; set; } = string.Empty;

    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public IssueType Type { get; set; }
    public IssueStatus Status { get; set; }
    public Priority Priority { get; set; }

    public int? StoryPoints { get; set; }
    public DateTime? DueDate { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}