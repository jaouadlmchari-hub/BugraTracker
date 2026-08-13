

namespace BugTracker.Application.DTOs.Sprints
{
    public class CreateSprintDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
