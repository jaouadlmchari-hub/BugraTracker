using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BugTracker.Domain.Common;

namespace BugTracker.Domain.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; 
        public string? Description { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
     
        public Guid OwnerId { get; set; }


        // --- Navigation Properties ---
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
        public virtual ICollection<Epic> Epics { get; set; } = new List<Epic>();
        public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
    }
}
