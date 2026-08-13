using BugTracker.Application.DTOs.Projects;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings;

public static class ProjectMappingExtensions
{
    public static ProjectDto ToDto(this Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            Status = project.Status,
            OwnerId = project.OwnerId,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}