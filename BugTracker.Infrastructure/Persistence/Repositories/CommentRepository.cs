using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class CommentRepository : Repository<Comment> , ICommentRepository
    {
        public CommentRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<IEnumerable<Comment>> GetByIssueIdAsync(Guid issueId)
        {
            return await _dbSet
                         .Where(c => c.IssueId == issueId)
                         .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetByUserIdAsync(Guid projectId, Guid userId)
        {
           return await _dbSet
                        .Where(c => c.AuthorId == userId &&
                                    c.Issue.ProjectId == projectId)
                        .ToListAsync();
        }
    }
}
