using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;


namespace BugTracker.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BugTrackerDbContext _context;

        public IUserRepository Users { get; }
        public IProjectRepository Projects { get; }
        public IProjectMemberRepository ProjectMembers { get; }
        public ISprintRepository Sprints { get; }
        public IEpicRepository Epics { get; }
        public IIssueRepository Issues { get; }
        public ICommentRepository Comments { get; }
        public IAttachmentRepository Attachments { get; }
        public IActivityLogRepository ActivityLogs { get; }
        public IRefreshTokenRepository RefreshTokens { get; }

        public UnitOfWork(
            BugTrackerDbContext context,
            IUserRepository users,
            IProjectRepository projects,
            IProjectMemberRepository projectMembers,
            ISprintRepository sprints,
            IEpicRepository epics,
            IIssueRepository issues,
            ICommentRepository comments,
            IAttachmentRepository attachments,
            IActivityLogRepository activityLogs,
            IRefreshTokenRepository refreshTokens)
        {
            _context = context;

            Users = users;
            Projects = projects;
            ProjectMembers = projectMembers;
            Sprints = sprints;
            Epics = epics;
            Issues = issues;
            Comments = comments;
            Attachments = attachments;
            ActivityLogs = activityLogs;
            RefreshTokens = refreshTokens;
        }


        public async Task<ITransaction> BeginTransactionAsync()
        {
            var transaction = await _context.Database.BeginTransactionAsync();

            return new EfTransaction(transaction);
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
