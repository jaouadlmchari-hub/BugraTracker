using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Exceptions;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceReorderDeleteTests : IssueServiceTestBase
    {
        [Fact]
        public async Task ReorderAsync_WhenItemsAreEmpty_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var items = new List<ReorderIssueItemDto>();

            // Act
            Func<Task> act = () => Sut.ReorderAsync(items);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>()
                .WithMessage("La liste des Issues à réordonner est vide.");

            IssueRepositoryMock.Verify(
                r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()),
                Times.Never);

            UnitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ReorderAsync_WhenIssueIdsContainDuplicates_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var items = new List<ReorderIssueItemDto>
                {
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId,
                        DisplayOrder = 1
                    },
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId,
                        DisplayOrder = 2
                    }
                };

            // Act
            Func<Task> act = () => Sut.ReorderAsync(items);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            IssueRepositoryMock.Verify(
                r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()),
                Times.Never);

            UnitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ReorderAsync_WhenOneOrMoreIssuesDoNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId1 = Guid.NewGuid();
            var issueId2 = Guid.NewGuid();

            var items = new List<ReorderIssueItemDto>
                {
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId1,
                        DisplayOrder = 1
                    },
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId2,
                        DisplayOrder = 2
                    }
                };

            var existingIssues = new List<Issue>
                {
                    new Issue
                    {
                        Id = issueId1,
                        ProjectId = Guid.NewGuid(),
                        Title = "Issue 1",
                        DisplayOrder = 10
                    }
                };

            IssueRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
                    ids.Contains(issueId1) &&
                    ids.Contains(issueId2))))
                .ReturnsAsync(existingIssues);

            // Act
            Func<Task> act = () => Sut.ReorderAsync(items);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            existingIssues[0].DisplayOrder.Should().Be(10);

            UnitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ReorderAsync_WhenIssuesBelongToDifferentProjects_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId1 = Guid.NewGuid();
            var issueId2 = Guid.NewGuid();
            var projectId1 = Guid.NewGuid();
            var projectId2 = Guid.NewGuid();

            var items = new List<ReorderIssueItemDto>
                {
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId1,
                        DisplayOrder = 1
                    },
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId2,
                        DisplayOrder = 2
                    }
               };

            var issues = new List<Issue>
               {
                  new Issue
                  {
                      Id = issueId1,
                      ProjectId = projectId1,
                      Title = "Issue 1",
                      DisplayOrder = 10
                  },
                  new Issue
                  {
                      Id = issueId2,
                      ProjectId = projectId2,
                      Title = "Issue 2",
                      DisplayOrder = 20
                  }
               };

            IssueRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(issues);

            // Act
            Func<Task> act = () => Sut.ReorderAsync(items);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            issues[0].DisplayOrder.Should().Be(10);
            issues[1].DisplayOrder.Should().Be(20);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ReorderAsync_WhenDataIsValid_ShouldUpdateDisplayOrderForAllIssues()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var issueId1 = Guid.NewGuid();
            var issueId2 = Guid.NewGuid();
            var issueId3 = Guid.NewGuid();

            var items = new List<ReorderIssueItemDto>
                {
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId1,
                        DisplayOrder = 3
                    },
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId2,
                        DisplayOrder = 1
                    },
                    new ReorderIssueItemDto
                    {
                        IssueId = issueId3,
                        DisplayOrder = 2
                    }
                };

            var issues = new List<Issue>
                {
                    new Issue
                    {
                        Id = issueId1,
                        ProjectId = projectId,
                        Title = "Issue 1",
                        DisplayOrder = 1
                    },
                    new Issue
                    {
                        Id = issueId2,
                        ProjectId = projectId,
                        Title = "Issue 2",
                        DisplayOrder = 2
                    },
                    new Issue
                    {
                        Id = issueId3,
                        ProjectId = projectId,
                        Title = "Issue 3",
                        DisplayOrder = 3
                    }
                };

            IssueRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(issues);

            // Act
            await Sut.ReorderAsync(items);

            // Assert
            issues.Single(i => i.Id == issueId1).DisplayOrder.Should().Be(3);
            issues.Single(i => i.Id == issueId2).DisplayOrder.Should().Be(1);
            issues.Single(i => i.Id == issueId3).DisplayOrder.Should().Be(2);

            IssueRepositoryMock.Verify(
                r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
                    ids.Contains(issueId1) &&
                    ids.Contains(issueId2) &&
                    ids.Contains(issueId3))),
                Times.Once);

            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => Sut.DeleteAsync(issueId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            IssueRepositoryMock.Verify(r => r.Delete(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenIssueExists_ShouldDeleteIssue()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            IssueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            await Sut.DeleteAsync(issueId);

            // Assert
            IssueRepositoryMock.Verify(r => r.Delete(issue), Times.Once);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
