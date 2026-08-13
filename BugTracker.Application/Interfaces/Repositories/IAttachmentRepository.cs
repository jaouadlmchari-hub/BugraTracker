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
        Task<int> CountByIssueIdAsync(Guid issueId);

        Task<IEnumerable<Attachment>> GetByIssueIdAsync(Guid issueId);

        Task<Attachment?> GetByIdWithDetailsAsync(Guid attachmentId);

        Task<IEnumerable<Attachment>> GetByUploaderIdAsync(Guid projectId, Guid uploaderId);
    }
}
