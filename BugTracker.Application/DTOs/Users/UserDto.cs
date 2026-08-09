using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public SystemRole SystemRole { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}