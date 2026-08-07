using BugTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{

    public interface IAttachmentRepository : IRepository<Attachment>
    {
        Task<IEnumerable<Attachment>> GetByIssueIdAsync(Guid issueId);

        Task<IEnumerable<Attachment>> GetByUploaderIdAsync(Guid projectId, Guid uploaderId);
    }
}
