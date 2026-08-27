using BugTracker.Application.Interfaces.Repositories;


namespace BugTracker.Application.Interfaces.Persistence
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

        Task<ITransaction> BeginTransactionAsync();

        Task<int> SaveChangesAsync();
    }
}
