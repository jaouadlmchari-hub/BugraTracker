using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;


namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceReadTests : IssueServiceTestBase
    {
        [Fact]
        public async Task GetByIdAsync_WhenIssueExists_ShouldReturnIssueDto()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = projectId,
                Title = "Login button not working",
                Description = "The login button does not respond.",
                Type = IssueType.Bug,
                Status = IssueStatus.Todo,
                Priority = Priority.High
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            var result = await Sut.GetByIdAsync(issueId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(issueId);
            result.ProjectId.Should().Be(projectId);
            result.Title.Should().Be("Login button not working");
            result.Type.Should().Be(IssueType.Bug);
            result.Status.Should().Be(IssueStatus.Todo);
            result.Priority.Should().Be(Priority.High);

            IssueRepositoryMock.Verify(r => r.GetByIdWithDetailsAsync(issueId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenIssueDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            var result = await Sut.GetByIdAsync(issueId);

            // Assert
            result.Should().BeNull();

            IssueRepositoryMock.Verify(r => r.GetByIdWithDetailsAsync(issueId), Times.Once);
        }

        [Fact]
        public async Task GetByProjectPaginatedAsync_ShouldReturnPagedIssues()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var filter = new IssueFilterDto
            {
                PageNumber = 2,
                PageSize = 10
            };

            var issues = new List<Issue>
                {
                    new Issue
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Title = "Login bug",
                        Type = IssueType.Bug,
                        Status = IssueStatus.Todo,
                        Priority = Priority.High
                    },
                    new Issue
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Title = "Create dashboard",
                        Type = IssueType.Task,
                        Status = IssueStatus.InProgress,
                        Priority = Priority.Medium
                    }
                };

            const int totalCount = 25;

            IssueRepositoryMock
                .Setup(r => r.GetPaginatedAsync(projectId, filter))
                .ReturnsAsync((issues, totalCount));

            // Act
            var result = await Sut.GetByProjectPaginatedAsync(projectId, filter);

            // Assert
            result.Items.Should().HaveCount(2);

            result.Items.Should().Contain(i => i.Title == "Login bug");
            result.Items.Should().Contain(i => i.Title == "Create dashboard");

            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);

            IssueRepositoryMock.Verify(r => r.GetPaginatedAsync(projectId, filter), Times.Once);
        }

    }
}
