using BugTracker.Application.DTOs.Sprints;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings
{
    public static  class SprintMappingExtensions
    {
        public static SprintDto ToDto(this Sprint sprint)
        {
            return new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                Status = sprint.Status,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                CompletedAt = sprint.CompletedAt,

            };
        }
    }
}
