using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Projects;

public class ProjectFilterDto
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public ProjectStatus? Status { get; set; }

}