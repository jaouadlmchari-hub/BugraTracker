using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Projects;

public class ProjectDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}