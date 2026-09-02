using BugTracker.Application.Exceptions;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceSprintTests : IssueServiceTestBase
    {
        [Fact]
        public async Task MoveToSprintAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            SprintRepositoryMock.Verify(
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
        public async Task MoveToSprintAsync_WhenSprintDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = null
            };


            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            Func<Task> act = () => Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            issue.SprintId.Should().BeNull();

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
        public async Task MoveToSprintAsync_WhenSprintBelongsToAnotherProject_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var anotherProjectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = null
            };

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = anotherProjectId,
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);


            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.SprintId.Should().BeNull();

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
        public async Task MoveToSprintAsync_WhenSprintIsCompleted_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = null
            };

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Completed
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.SprintId.Should().BeNull();

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
        public async Task MoveToSprintAsync_WhenIssueIsAlreadyInSameSprint_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = sprintId
            };

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.SprintId.Should().Be(sprintId);

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
        public async Task MoveToSprintAsync_WhenIssueIsAlreadyInBacklog_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug",
                SprintId = null
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            Func<Task> act = () => Sut.MoveToSprintAsync(issueId, null);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.SprintId.Should().BeNull();

            SprintRepositoryMock.Verify(
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
        public async Task MoveToSprintAsync_WhenSprintIsValid_ShouldMoveIssueToSprintAndCreateActivityLog()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = null
            };

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            await Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            issue.SprintId.Should().Be(sprintId);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.SprintChanged,
                    "Sprint",
                    null,
                    sprintId.ToString()),
                Times.Once);

            UnitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task MoveToSprintAsync_WhenMovingFromBacklogToValidSprint_ShouldMoveIssueAndCreateActivityLog()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = null
            };

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            // Act
            await Sut.MoveToSprintAsync(issueId, sprintId);

            // Assert
            issue.SprintId.Should().Be(sprintId);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.SprintChanged,
                    "Sprint",
                    null,
                    sprintId.ToString()),
                Times.Once);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MoveToSprintAsync_WhenMovingToAnotherValidSprint_ShouldUpdateSprintAndCreateActivityLog()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var oldSprintId = Guid.NewGuid();
            var newSprintId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                SprintId = oldSprintId
            };

            var newSprint = new Sprint
            {
                Id = newSprintId,
                ProjectId = projectId,
                Name = "Sprint 2",
                Status = SprintStatus.Planning
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(newSprintId))
                .ReturnsAsync(newSprint);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            // Act
            await Sut.MoveToSprintAsync(issueId, newSprintId);

            // Assert
            issue.SprintId.Should().Be(newSprintId);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.SprintChanged,
                    "Sprint",
                    oldSprintId.ToString(),
                    newSprintId.ToString()),
                Times.Once);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

    }
}
