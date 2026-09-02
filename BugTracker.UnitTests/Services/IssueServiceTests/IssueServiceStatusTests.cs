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
using Xunit;

namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceStatusTests : IssueServiceTestBase
    {
        [Fact]
        public async Task ChangeStatusAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.ChangeStatusAsync(issueId, IssueStatus.InProgress);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

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
        public async Task ChangeStatusAsync_WhenTransitionIsInvalid_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug",
                Type = IssueType.Bug,
                Status = IssueStatus.Todo
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            Func<Task> act = () => Sut.ChangeStatusAsync(issueId, IssueStatus.Done);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>()
                .WithMessage("INVALID_STATUS_TRANSITION");

            issue.Status.Should().Be(IssueStatus.Todo);

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
        public async Task ChangeStatusAsync_WhenNewStatusIsInvalid_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug",
                Status = IssueStatus.Todo
            };

            var invalidStatus = (IssueStatus)999;

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            Func<Task> act = () => Sut.ChangeStatusAsync(issueId, invalidStatus);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.Status.Should().Be(IssueStatus.Todo);

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
        public async Task ChangeStatusAsync_WhenIssueBelongsToCompletedSprint_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                SprintId = sprintId,
                Title = "Login bug",
                Type = IssueType.Bug,
                Status = IssueStatus.Todo
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
            Func<Task> act = () => Sut.ChangeStatusAsync(issueId, IssueStatus.InProgress);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.Status.Should().Be(IssueStatus.Todo);

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

        [Theory]
        [InlineData(ProjectRole.Manager, IssueType.Bug, false)]
        [InlineData(ProjectRole.Manager, IssueType.Task, false)]
        [InlineData(ProjectRole.QA, IssueType.Bug, false)]
        public async Task ChangeStatusAsync_WhenAuthorizedProjectMemberReopensDoneIssue_ShouldChangeStatus(ProjectRole role, IssueType issueType, bool isAdmin)
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Issue",
                Type = issueType,
                Status = IssueStatus.Done
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(userId);

            CurrentUserServiceMock
                .SetupGet(c => c.IsAdmin)
                .Returns(isAdmin);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            // Act
            await Sut.ChangeStatusAsync(issueId, IssueStatus.Todo);

            // Assert
            issue.Status.Should().Be(IssueStatus.Todo);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    userId,
                    ActivityAction.StatusChanged,
                    "Status",
                    IssueStatus.Done.ToString(),
                    IssueStatus.Todo.ToString()),
                Times.Once);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(ProjectRole.Developer, IssueType.Bug)]
        [InlineData(ProjectRole.Developer, IssueType.Task)]
        [InlineData(ProjectRole.QA, IssueType.Task)]
        public async Task ChangeStatusAsync_WhenUnauthorizedProjectMemberReopensDoneIssue_ShouldThrowForbiddenException(ProjectRole role, IssueType issueType)
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Issue",
                Type = issueType,
                Status = IssueStatus.Done
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(userId);

            CurrentUserServiceMock
                .SetupGet(c => c.IsAdmin)
                .Returns(false);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            // Act
            Func<Task> act = () => Sut.ChangeStatusAsync(issueId, IssueStatus.Todo);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();

            issue.Status.Should().Be(IssueStatus.Done);

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
        public async Task ChangeStatusAsync_WhenAdminReopensDoneIssue_ShouldChangeStatusWithoutCheckingProjectMembership()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                Type = IssueType.Bug,
                Status = IssueStatus.Done
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(userId);

            CurrentUserServiceMock
                .SetupGet(c => c.IsAdmin)
                .Returns(true);

            // Act
            await Sut.ChangeStatusAsync(issueId, IssueStatus.Todo);

            // Assert
            issue.Status.Should().Be(IssueStatus.Todo);

            ProjectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    userId,
                    ActivityAction.StatusChanged,
                    "Status",
                    IssueStatus.Done.ToString(),
                    IssueStatus.Todo.ToString()),
                Times.Once);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}