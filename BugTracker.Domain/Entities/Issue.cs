using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BugTracker.Domain.Common;

namespace BugTracker.Domain.Entities
{
    public class Issue : BaseEntity
    {

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } 

        // Enums
        public IssueType Type { get; set; }     // Bug, Feature, Task
        public IssueStatus Status { get; set; } // Todo, InProgress, InReview, Done
        public Priority Priority { get; set; }  // Low, Medium, High, Urgent

        public int? StoryPoints { get; set; }      // Points d'estimation (Ex: 1, 3, 5)
        public DateTime? DueDate { get; set; }     // Date d'échéance
        public int DisplayOrder { get; set; } = 0; // Ordre d'affichage dans la colonne Kanban

        public Guid ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;

        public Guid? EpicId { get; set; }
        public virtual Epic? Epic { get; set; }

      
        public Guid? SprintId { get; set; }
        public virtual Sprint? Sprint { get; set; }

    
        public Guid ReporterId { get; set; }
        public virtual User Reporter { get; set; } = null!;

  
        public Guid? AssigneeId { get; set; }
        public virtual User? Assignee { get; set; }

        // Collections
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    }
}
