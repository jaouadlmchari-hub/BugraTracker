using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.ActivityLogs;

public class ActivityLogDto
{
    public Guid Id { get; set; }

    public Guid IssueId { get; set; }

    public Guid UserId { get; set; }

    public string? UserName { get; set; }

    public ActivityAction Action { get; set; }

    public string? Field { get; set; }

    public string? FromValue { get; set; }

    public string? ToValue { get; set; }

    public DateTime CreatedAt { get; set; }
}