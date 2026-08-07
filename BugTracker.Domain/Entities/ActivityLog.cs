using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Domain.Entities
{
    public class ActivityLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid IssueId { get; set; }
        public Guid UserId { get; set; }

        public ActivityAction Action { get; set; }

        // Détails du changement (Optionnels)
        public string? Field { get; set; }
        public string? FromValue { get; set; }
        public string? ToValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        public virtual Issue Issue { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
