using BugTracker.Application.Exceptions;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceEpicTests : IssueServiceTestBase
    {

        [Fact]
        public async Task MoveToEpicAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.MoveToEpicAsync(issueId, epicId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            EpicRepositoryMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MoveToEpicAsync_WhenEpicDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                EpicId = null
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            Func<Task> act = () => Sut.MoveToEpicAsync(issueId, epicId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            issue.EpicId.Should().BeNull();

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MoveToEpicAsync_WhenEpicBelongsToAnotherProject_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var anotherProjectId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                EpicId = null
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = anotherProjectId,
                Title = "Authentication",
                Status = EpicStatus.Active
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            Func<Task> act = () => Sut.MoveToEpicAsync(issueId, epicId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.EpicId.Should().BeNull();

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MoveToEpicAsync_WhenEpicIsArchived_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                EpicId = null
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = projectId,
                Title = "Authentication",
                Status = EpicStatus.Archived
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            Func<Task> act = () => Sut.MoveToEpicAsync(issueId, epicId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.EpicId.Should().BeNull();

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MoveToEpicAsync_WhenEpicIsValid_ShouldMoveIssueToEpic()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                EpicId = null
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = projectId,
                Title = "Authentication",
                Status = EpicStatus.Active
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            await Sut.MoveToEpicAsync(issueId, epicId);

            // Assert
            issue.EpicId.Should().Be(epicId);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MoveToEpicAsync_WhenEpicIdIsNull_ShouldRemoveIssueFromEpic()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var currentEpicId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug",
                EpicId = currentEpicId
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            await Sut.MoveToEpicAsync(issueId, null);

            // Assert
            issue.EpicId.Should().BeNull();

            EpicRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
