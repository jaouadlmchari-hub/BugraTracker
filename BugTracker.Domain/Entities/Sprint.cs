using BugTracker.Domain.Enums;
using BugTracker.Domain.Common;

namespace BugTracker.Domain.Entities
{
    public class Sprint : BaseEntity
    {
        public Guid ProjectId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }

        public SprintStatus Status { get; set; } = SprintStatus.Planning;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        // --- Navigation Properties ---
        public virtual Project Project { get; set; } = null!;
        public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
    }
}
