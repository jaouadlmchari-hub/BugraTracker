using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BugTracker.UnitTests.Services
{
    public class ActivityLogServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock;
        private readonly Mock<IIssueRepository> _issueRepositoryMock;
        private readonly ActivityLogService _sut;

        public ActivityLogServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _activityLogRepositoryMock = new Mock<IActivityLogRepository>();
            _issueRepositoryMock = new Mock<IIssueRepository>();

            _unitOfWorkMock
                .SetupGet(u => u.ActivityLogs)
                .Returns(_activityLogRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Issues)
                .Returns(_issueRepositoryMock.Object);

            _sut = new ActivityLogService(_unitOfWorkMock.Object);
        }


        [Fact]
        public async Task GetByIssueAsync_WhenIssueExists_ShouldReturnActivityLogs()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com"
            };

            var logs = new List<ActivityLog>
                {
                    new ActivityLog
                    {
                        Id = Guid.NewGuid(),
                        IssueId = issueId,
                        UserId = userId,
                        User = user,
                        Action = ActivityAction.StatusChanged,
                        Field = "Status",
                        FromValue = IssueStatus.Todo.ToString(),
                        ToValue = IssueStatus.InProgress.ToString(),
                        CreatedAt = DateTime.UtcNow
                    },
                    new ActivityLog
                    {
                        Id = Guid.NewGuid(),
                        IssueId = issueId,
                        UserId = userId,
                        User = user,
                        Action = ActivityAction.Commented,
                        CreatedAt = DateTime.UtcNow
                    }
                };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            _activityLogRepositoryMock
                .Setup(r => r.GetByIssueIdAsync(issueId))
                .ReturnsAsync(logs);

            // Act
            var result = (await _sut.GetByIssueAsync(issueId)).ToList();

            // Assert
            result.Should().HaveCount(2);

            result[0].IssueId.Should().Be(issueId);
            result[0].UserId.Should().Be(userId);
            result[0].Action.Should().Be(ActivityAction.StatusChanged);
            result[0].Field.Should().Be("Status");
            result[0].FromValue.Should().Be(IssueStatus.Todo.ToString());
            result[0].ToValue.Should().Be(IssueStatus.InProgress.ToString());

            result[1].Action.Should().Be(ActivityAction.Commented);

            _issueRepositoryMock.Verify(r => r.GetByIdAsync(issueId), Times.Once);
            _activityLogRepositoryMock.Verify(r => r.GetByIssueIdAsync(issueId), Times.Once);
        }

        [Fact]
        public async Task GetByIssueAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => _sut.GetByIssueAsync(issueId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _activityLogRepositoryMock.Verify(
                r => r.GetByIssueIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task LogAsync_ShouldCreateActivityLog()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            ActivityLog? createdLog = null;

            _activityLogRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ActivityLog>()))
                .Callback<ActivityLog>(log => createdLog = log)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.LogAsync(
                issueId,
                userId,
                ActivityAction.StatusChanged,
                "Status",
                IssueStatus.Todo.ToString(),
                IssueStatus.InProgress.ToString());

            // Assert
            createdLog.Should().NotBeNull();

            createdLog!.IssueId.Should().Be(issueId);
            createdLog.UserId.Should().Be(userId);
            createdLog.Action.Should().Be(ActivityAction.StatusChanged);
            createdLog.Field.Should().Be("Status");
            createdLog.FromValue.Should().Be(IssueStatus.Todo.ToString());
            createdLog.ToValue.Should().Be(IssueStatus.InProgress.ToString());

            _activityLogRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ActivityLog>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }
    }
}
