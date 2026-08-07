using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Domain.Entities
{
    public class ProjectMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();

       
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }


        public ProjectRole Role { get; set; } = ProjectRole.Developer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        public virtual Project Project { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
