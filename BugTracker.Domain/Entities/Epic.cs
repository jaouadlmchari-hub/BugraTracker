using BugTracker.Domain.Common;
using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Domain.Entities
{
    public class Epic : BaseEntity
    {

        public Guid ProjectId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
  
        public string ColorCode { get; set; } = "#3B82F6";

        public EpicStatus Status { get; set; }

        public virtual Project Project { get; set; } = null!;

        public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
    }
}
