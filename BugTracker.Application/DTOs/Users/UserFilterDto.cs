namespace BugTracker.Application.DTOs.Users;

public class UserFilterDto
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}