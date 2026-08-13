using BugTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Repositories
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetByIssueIdAsync(Guid issueId);

        Task<Comment?> GetByIdWithDetailsAsync(Guid commentId);

        Task<IEnumerable<Comment>> GetByUserIdAsync(Guid projectId, Guid userId);
    }
}
