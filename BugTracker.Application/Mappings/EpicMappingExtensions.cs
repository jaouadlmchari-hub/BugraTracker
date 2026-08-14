using BugTracker.Application.DTOs.Epics;
using BugTracker.Domain.Entities;


namespace BugTracker.Application.Mappings
{
    public static  class EpicMappingExtensions
    {
        public static EpicDto ToDto(this Epic epic)
        {
            return new EpicDto
            {
                Id = epic.Id,
                ProjectId = epic.ProjectId,
                Title = epic.Title,
                Description = epic.Description,
                ColorCode = epic.ColorCode,
                Status = epic.Status,
            };
        }

        public static EpicDetailsDto ToDetailsDto(this Epic epic)
        {
            return new EpicDetailsDto
            {
                Id = epic.Id,
                ProjectId = epic.ProjectId,
                Title = epic.Title,
                Description = epic.Description,
                ColorCode = epic.ColorCode,
                Status = epic.Status,

                Issues = epic.Issues
                    .Select(i => i.ToDto())
                    .ToList()
            };
        }
    }
}
