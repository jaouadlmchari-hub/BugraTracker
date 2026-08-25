using BugTracker.Domain.Enums;


namespace BugTracker.Application.DTOs.Epics
{
    public class ChangeEpicStatusDto
    {
        public EpicStatus NewStatus { get; set; }
    }
}
