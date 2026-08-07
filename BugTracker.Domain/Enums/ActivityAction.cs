using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Domain.Enums
{
    public enum ActivityAction
    {
        Created,
        TitleChanged,
        DescriptionChanged,
        StatusChanged,
        PriorityChanged,
        Assigned,
        SprintChanged,
        Commented,
        AttachmentAdded,
        AttachmentRemoved
    }
}
