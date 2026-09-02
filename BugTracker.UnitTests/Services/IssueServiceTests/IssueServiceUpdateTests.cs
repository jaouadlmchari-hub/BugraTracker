using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Exceptions;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;


namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceUpdateTests : IssueServiceTestBase
    {
        [Fact]
        public async Task UpdateAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var dto = new UpdateIssueDto
            {
                Title = "Updated title",
                Description = "Updated description",
                Type = IssueType.Bug,
                Priority = Priority.High,
                StoryPoints = 5
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.UpdateAsync(issueId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNewEpicDoesNotExist_ShouldThrowNotFoundException()
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
                Description = "Old description",
                Type = IssueType.Bug,
                Status = IssueStatus.Todo,
                Priority = Priority.High,
                EpicId = null
            };

            var dto = new UpdateIssueDto
            {
                Title = "Updated login bug",
                Description = "Updated description",
                Type = IssueType.Bug,
                Priority = Priority.High,
                StoryPoints = 5,
                EpicId = epicId
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            Func<Task> act = () => Sut.UpdateAsync(issueId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            issue.EpicId.Should().BeNull();

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNewEpicBelongsToAnotherProject_ShouldThrowBusinessRuleException()
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
                Description = "Old description",
                Type = IssueType.Bug,
                Status = IssueStatus.Todo,
                Priority = Priority.High,
                EpicId = null
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = anotherProjectId,
                Title = "Authentication",
                Status = EpicStatus.Active
            };

            var dto = new UpdateIssueDto
            {
                Title = "Updated login bug",
                Description = "Updated description",
                Type = IssueType.Bug,
                Priority = Priority.High,
                StoryPoints = 5,
                EpicId = epicId
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            Func<Task> act = () => Sut.UpdateAsync(issueId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issue.EpicId.Should().BeNull();

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenDataIsValid_ShouldUpdateIssue()
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
                Description = "Old description",
                Type = IssueType.Bug,
                Status = IssueStatus.Todo,
                Priority = Priority.Medium,
                StoryPoints = 3,
                EpicId = null
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = projectId,
                Title = "Authentication",
                Status = EpicStatus.Active
            };

            var dto = new UpdateIssueDto
            {
                Title = "Updated login bug",
                Description = "Updated description",
                Type = IssueType.Task,
                Priority = Priority.High,
                StoryPoints = 5,
                EpicId = epicId
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            var beforeUpdate = DateTime.UtcNow;

            // Act
            var result = await Sut.UpdateAsync(issueId, dto);

            // Assert
            issue.Title.Should().Be(dto.Title);
            issue.Description.Should().Be(dto.Description);
            issue.Type.Should().Be(dto.Type);
            issue.Priority.Should().Be(dto.Priority);
            issue.StoryPoints.Should().Be(dto.StoryPoints);
            issue.EpicId.Should().Be(epicId);
            issue.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);

            result.Title.Should().Be(dto.Title);
            result.Description.Should().Be(dto.Description);
            result.Type.Should().Be(dto.Type);
            result.Priority.Should().Be(dto.Priority);
            result.StoryPoints.Should().Be(dto.StoryPoints);
            result.EpicId.Should().Be(epicId);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangeStoryPointsAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.ChangeStoryPointsAsync(issueId, 5);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(8)]
        [InlineData(null)]
        public async Task ChangeStoryPointsAsync_WhenIssueExists_ShouldUpdateStoryPoints(int? storyPoints)
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug",
                StoryPoints = 3
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            await Sut.ChangeStoryPointsAsync(issueId, storyPoints);

            // Assert
            issue.StoryPoints.Should().Be(storyPoints);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}