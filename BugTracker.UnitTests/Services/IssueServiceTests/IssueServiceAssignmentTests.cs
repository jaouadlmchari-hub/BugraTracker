using BugTracker.Application.Exceptions;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;


namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceAssignmentTests : IssueServiceTestBase
    {
        [Fact]
        public async Task AssignAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.AssignAsync(issueId, userId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            ProjectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
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
        public async Task AssignAsync_WhenTargetUserIsNotProjectMember_ShouldThrowBusinessRuleException()
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
                AssigneeId = null
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync((ProjectMember?)null);

            // Act
            Func<Task> act = () => Sut.AssignAsync(issueId, userId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.AssigneeId.Should().BeNull();

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
        public async Task AssignAsync_WhenIssueIsAlreadyAssignedToUser_ShouldThrowBusinessRuleException()
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
                AssigneeId = userId
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Developer
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            // Act
            Func<Task> act = () => Sut.AssignAsync(issueId, userId);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.AssigneeId.Should().Be(userId);

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
        public async Task AssignAsync_WhenDataIsValid_ShouldAssignUserAndCreateActivityLog()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var oldAssigneeId = Guid.NewGuid();
            var newAssigneeId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login bug",
                AssigneeId = oldAssigneeId
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = newAssigneeId,
                Role = ProjectRole.Developer
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, newAssigneeId))
                .ReturnsAsync(member);

            // Act
            await Sut.AssignAsync(issueId, newAssigneeId);

            // Assert
            issue.AssigneeId.Should().Be(newAssigneeId);

            ActivityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.Assigned,
                    "Assignee",
                    oldAssigneeId.ToString(),
                    newAssigneeId.ToString()),
                Times.Once);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}