using BugTracker.Domain.Enums;

public class IssueFilterDto
{
    public string? Search { get; set; }

    public IssueStatus? Status { get; set; }

    public IssueType? Type { get; set; }

    public Priority? Priority { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid? ReporterId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid? EpicId { get; set; }

    public bool? BacklogOnly { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}