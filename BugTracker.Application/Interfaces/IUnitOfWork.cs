using BugTracker.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IProjectRepository Projects { get; }
        IProjectMemberRepository ProjectMembers { get; }
        ISprintRepository Sprints { get; }
        IEpicRepository Epics { get; }
        IIssueRepository Issues { get; }
        ICommentRepository Comments { get; }
        IAttachmentRepository Attachments { get; }
        IActivityLogRepository ActivityLogs { get; }
        IRefreshTokenRepository RefreshTokens { get; }

        Task<int> SaveChangesAsync();
    }
}
