using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Services;
using Moq;

namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public abstract class IssueServiceTestBase
    {
        protected readonly Mock<IUnitOfWork> UnitOfWorkMock;
        protected readonly Mock<IIssueRepository> IssueRepositoryMock;
        protected readonly Mock<IProjectRepository> ProjectRepositoryMock;
        protected readonly Mock<IProjectMemberRepository> ProjectMemberRepositoryMock;
        protected readonly Mock<ISprintRepository> SprintRepositoryMock;
        protected readonly Mock<IEpicRepository> EpicRepositoryMock;
        protected readonly Mock<ICurrentUserService> CurrentUserServiceMock;
        protected readonly Mock<IActivityLogService> ActivityLogServiceMock;
        protected readonly Mock<ITransaction> TransactionMock;
        protected readonly IssueService Sut;

        protected IssueServiceTestBase()
        {
            UnitOfWorkMock = new Mock<IUnitOfWork>();
            IssueRepositoryMock = new Mock<IIssueRepository>();
            ProjectRepositoryMock = new Mock<IProjectRepository>();
            ProjectMemberRepositoryMock = new Mock<IProjectMemberRepository>();
            SprintRepositoryMock = new Mock<ISprintRepository>();
            EpicRepositoryMock = new Mock<IEpicRepository>();
            CurrentUserServiceMock = new Mock<ICurrentUserService>();
            ActivityLogServiceMock = new Mock<IActivityLogService>();
            TransactionMock = new Mock<ITransaction>();

            UnitOfWorkMock.SetupGet(u => u.Issues).Returns(IssueRepositoryMock.Object);
            UnitOfWorkMock.SetupGet(u => u.Projects).Returns(ProjectRepositoryMock.Object);
            UnitOfWorkMock.SetupGet(u => u.ProjectMembers).Returns(ProjectMemberRepositoryMock.Object);
            UnitOfWorkMock.SetupGet(u => u.Sprints).Returns(SprintRepositoryMock.Object);
            UnitOfWorkMock.SetupGet(u => u.Epics).Returns(EpicRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(TransactionMock.Object);

            Sut = new IssueService(UnitOfWorkMock.Object, CurrentUserServiceMock.Object, ActivityLogServiceMock.Object);
        }
    }
}
