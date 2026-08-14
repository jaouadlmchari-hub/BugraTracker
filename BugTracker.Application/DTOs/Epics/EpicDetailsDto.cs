using BugTracker.Application.DTOs.Issues;
using BugTracker.Domain.Enums;


namespace BugTracker.Application.DTOs.Epics
{
    public class EpicDetailsDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string ColorCode { get; set; } = "#3B82F6";

        public EpicStatus Status { get; set; }

        public List<IssueDto> Issues { get; set; } = new();
    }
}
