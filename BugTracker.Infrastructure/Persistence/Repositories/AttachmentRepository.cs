using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class AttachmentRepository : Repository<Attachment> , IAttachmentRepository
    {
        public AttachmentRepository(BugTrackerDbContext context)  : base(context)
        {
        }

        public async Task<int> CountByIssueIdAsync(Guid issueId)
        {
            return await _context.Attachments
                .CountAsync(a => a.IssueId == issueId);
        }

        public async Task<IEnumerable<Attachment>> GetByIssueIdAsync(Guid issueId)
        {
            return await _dbSet
                         .Where(a => a.IssueId == issueId)
                         .ToListAsync();
        }

        public async Task<Attachment?> GetByIdWithDetailsAsync(Guid attachmentId)
        {
            return await _context.Attachments
                .Include(a => a.Issue)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);
        }

        public async Task<IEnumerable<Attachment>> GetByUploaderIdAsync(Guid projectId, Guid uploaderId)
        {
            return await _dbSet
                         .Where(a => a.UploaderId == uploaderId &&
                                     a.Issue.ProjectId == projectId)
                         .ToListAsync();
        }
    }
}
