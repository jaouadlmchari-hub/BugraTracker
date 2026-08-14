

namespace BugTracker.Application.DTOs.Epics
{
    public class CreateEpicDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string ColorCode { get; set; } = "#3B82F6";
    }
}
