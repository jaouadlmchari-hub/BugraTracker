using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BugTracker.Domain.Common;

namespace BugTracker.Domain.Entities
{
    public class Comment : BaseEntity
    {
        public Guid IssueId { get; set; }
        public Guid AuthorId { get; set; }

        public string Content { get; set; } = string.Empty;


        // --- Navigation Properties ---
        public virtual Issue Issue { get; set; } = null!;
        public virtual User Author { get; set; } = null!;
    }
}
