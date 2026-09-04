using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;

public class CommentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<IIssueRepository> _issueRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _issueRepositoryMock = new Mock<IIssueRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _activityLogServiceMock = new Mock<IActivityLogService>();

        _unitOfWorkMock.SetupGet(u => u.Comments).Returns(_commentRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Issues).Returns(_issueRepositoryMock.Object);

        _sut = new CommentService(_unitOfWorkMock.Object, _currentUserServiceMock.Object, _activityLogServiceMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCommentExists_ShouldReturnCommentDto()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var comment = new Comment
        {
            Id = commentId,
            IssueId = issueId,
            AuthorId = authorId,
            Content = "This bug happens after login."
        };

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _sut.GetByIdAsync(commentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(commentId);
        result.IssueId.Should().Be(issueId);
        result.AuthorId.Should().Be(authorId);
        result.Content.Should().Be("This bug happens after login");

        _commentRepositoryMock.Verify(r => r.GetByIdAsync(commentId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCommentDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var commentId = Guid.NewGuid();

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        var result = await _sut.GetByIdAsync(commentId);

        // Assert
        result.Should().BeNull();

        _commentRepositoryMock.Verify(r => r.GetByIdAsync(commentId), Times.Once);
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

        _commentRepositoryMock.Verify(
            r => r.GetByIssueIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIssueAsync_WhenIssueExists_ShouldReturnComments()
    {
        // Arrange
        var issueId = Guid.NewGuid();
        var authorId1 = Guid.NewGuid();
        var authorId2 = Guid.NewGuid();

        var issue = new Issue
        {
            Id = issueId,
            ProjectId = Guid.NewGuid(),
            Title = "Login bug"
        };

        var comments = new List<Comment>
           {
               new Comment
               {
                   Id = Guid.NewGuid(),
                   IssueId = issueId,
                   AuthorId = authorId1,
                   Content = "First comment"
               },
               new Comment
               {
                   Id = Guid.NewGuid(),
                   IssueId = issueId,
                   AuthorId = authorId2,
                   Content = "Second comment"
               }
           };

        _issueRepositoryMock
            .Setup(r => r.GetByIdAsync(issueId))
            .ReturnsAsync(issue);

        _commentRepositoryMock
            .Setup(r => r.GetByIssueIdAsync(issueId))
            .ReturnsAsync(comments);

        // Act
        var result = await _sut.GetByIssueAsync(issueId);

        // Assert
        result.Should().HaveCount(2);

        result.Should().Contain(c => c.Content == "First comment");
        result.Should().Contain(c => c.Content == "Second comment");

        _commentRepositoryMock.Verify(
            r => r.GetByIssueIdAsync(issueId),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var issueId = Guid.NewGuid();

        var dto = new CreateCommentDto
        {
            Content = "This is a comment"
        };

        _issueRepositoryMock
            .Setup(r => r.GetByIdAsync(issueId))
            .ReturnsAsync((Issue?)null);

        // Act
        Func<Task> act = () => _sut.CreateAsync(issueId, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _commentRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Comment>()),
            Times.Never);

        _activityLogServiceMock.Verify(
            a => a.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<ActivityAction>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDataIsValid_ShouldCreateCommentAndLogActivity()
    {
        // Arrange
        var issueId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var issue = new Issue
        {
            Id = issueId,
            ProjectId = projectId,
            Title = "Login bug"
        };

        var dto = new CreateCommentDto
        {
            Content = "I reproduced this bug locally."
        };

        _issueRepositoryMock
            .Setup(r => r.GetByIdAsync(issueId))
            .ReturnsAsync(issue);

        _currentUserServiceMock
            .SetupGet(c => c.UserId)
            .Returns(currentUserId);

        Comment? createdComment = null;

        _commentRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Comment>()))
            .Callback<Comment>(comment => createdComment = comment)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(issueId, dto);

        // Assert
        createdComment.Should().NotBeNull();

        createdComment!.IssueId.Should().Be(issueId);
        createdComment.AuthorId.Should().Be(currentUserId);
        createdComment.Content.Should().Be(dto.Content);

        result.IssueId.Should().Be(issueId);
        result.AuthorId.Should().Be(currentUserId);
        result.Content.Should().Be(dto.Content);

        _commentRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Comment>()),
            Times.Once);

        _activityLogServiceMock.Verify(
            a => a.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.Commented,
                null,
                null,
                null),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCommentDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var commentId = Guid.NewGuid();

        var dto = new UpdateCommentDto
        {
            Content = "Updated comment"
        };

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        Func<Task> act = () => _sut.UpdateAsync(commentId, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCommentIsOlderThan24Hours_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var commentId = Guid.NewGuid();

        var comment = new Comment
        {
            Id = commentId,
            IssueId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Original comment",
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };

        var dto = new UpdateCommentDto
        {
            Content = "Updated comment"
        };

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        Func<Task> act = () => _sut.UpdateAsync(commentId, dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();

        comment.Content.Should().Be("Original comment");

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCommentIsWithin24Hours_ShouldUpdateComment()
    {
        // Arrange
        var commentId = Guid.NewGuid();

        var comment = new Comment
        {
            Id = commentId,
            IssueId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Original comment",
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var dto = new UpdateCommentDto
        {
            Content = "Updated comment"
        };

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        // Act
        var result = await _sut.UpdateAsync(commentId, dto);

        // Assert
        comment.Content.Should().Be(dto.Content);

        result.Content.Should().Be(dto.Content);
        result.Id.Should().Be(commentId);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCommentDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var commentId = Guid.NewGuid();

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync((Comment?)null);

        // Act
        Func<Task> act = () => _sut.DeleteAsync(commentId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _commentRepositoryMock.Verify(
            r => r.Delete(It.IsAny<Comment>()),
            Times.Never);

        _activityLogServiceMock.Verify(
            a => a.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<ActivityAction>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCommentExists_ShouldDeleteCommentAndLogActivity()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var comment = new Comment
        {
            Id = commentId,
            IssueId = issueId,
            AuthorId = Guid.NewGuid(),
            Content = "Comment to delete"
        };

        _commentRepositoryMock
            .Setup(r => r.GetByIdAsync(commentId))
            .ReturnsAsync(comment);

        _currentUserServiceMock
            .SetupGet(c => c.UserId)
            .Returns(currentUserId);

        // Act
        await _sut.DeleteAsync(commentId);

        // Assert
        _activityLogServiceMock.Verify(
            a => a.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.CommentDeleted,
                null,
                null,
                null),
            Times.Once);

        _commentRepositoryMock.Verify(
            r => r.Delete(comment),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(),
            Times.Once);
    }

}
