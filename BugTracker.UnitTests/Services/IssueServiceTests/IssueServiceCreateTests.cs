using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Exceptions;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BugTracker.UnitTests.Services.IssueServiceTests
{
    public class IssueServiceCreateTests : IssueServiceTestBase
    {
        [Fact]
        public async Task CreateAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High
            };

            ProjectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => Sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Never);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Once);

            AuditServiceMock.Verify(
                a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenSprintDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High,
                SprintId = sprintId
            };

            ProjectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            SprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            Func<Task> act = () => Sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Never);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Once);

            AuditServiceMock.Verify(
                a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenSprintBelongsToAnotherProject_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var anotherProjectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = anotherProjectId,
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High,
                SprintId = sprintId
            };

            ProjectRepositoryMock.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);
            SprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId)).ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => Sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Never);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Once);

            AuditServiceMock.Verify(
                a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEpicDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High,
                EpicId = epicId
            };

            ProjectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            Func<Task> act = () => Sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Never);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Once);

            AuditServiceMock.Verify(
                a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEpicBelongsToAnotherProject_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var anotherProjectId = Guid.NewGuid();
            var epicId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = anotherProjectId,
                Title = "Authentication",
                Status = EpicStatus.Active
            };

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High,
                EpicId = epicId
            };

            ProjectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            EpicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            Func<Task> act = () => Sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Never);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Once);

            AuditServiceMock.Verify(
                a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenAssigneeIsNotProjectMember_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High,
                AssigneeId = assigneeId
            };

            ProjectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, assigneeId))
                .ReturnsAsync((ProjectMember?)null);

            // Act
            Func<Task> act = () => Sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Never);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Never);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Once);

            AuditServiceMock.Verify(
                a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenDataIsValid_ShouldCreateIssue()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var reporterId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = assigneeId,
                Role = ProjectRole.Developer
            };

            var reporter = new User
            {
                Id = reporterId,
                Username = "reporter"
            };

            var assignee = new User
            {
                Id = assigneeId,
                Username = "assignee"
            };

            var dto = new CreateIssueDto
            {
                Title = "Login bug",
                Description = "Login button is not working",
                Type = IssueType.Bug,
                Priority = Priority.High,
                AssigneeId = assigneeId
            };

            CurrentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(reporterId);

            ProjectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            ProjectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, assigneeId))
                .ReturnsAsync(projectMember);

            Issue? createdIssue = null;

            IssueRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Issue>()))
                .Callback<Issue>(issue => createdIssue = issue)
                .Returns(Task.CompletedTask);

            IssueRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    if (createdIssue?.Id == id)
                    {
                        createdIssue.Reporter = reporter;
                        createdIssue.Assignee = assignee;
                        return createdIssue;
                    }
                    return null;
                });

            UnitOfWorkMock
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(0);

            ActivityLogServiceMock
                .Setup(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ActivityAction>(), null, null, null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await Sut.CreateAsync(projectId, dto);

            // Assert
            createdIssue.Should().NotBeNull();

            createdIssue!.ProjectId.Should().Be(projectId);
            createdIssue.ReporterId.Should().Be(reporterId);
            createdIssue.AssigneeId.Should().Be(assigneeId);

            createdIssue.Title.Should().Be(dto.Title);
            createdIssue.Description.Should().Be(dto.Description);
            createdIssue.Type.Should().Be(IssueType.Bug);
            createdIssue.Priority.Should().Be(Priority.High);
            createdIssue.Status.Should().Be(IssueStatus.Todo);

            result.Title.Should().Be(dto.Title);
            result.ProjectId.Should().Be(projectId);
            result.ReporterId.Should().Be(reporterId);
            result.AssigneeId.Should().Be(assigneeId);

            IssueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Issue>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
            TransactionMock.Verify(t => t.CommitAsync(), Times.Once);
            TransactionMock.Verify(t => t.RollbackAsync(), Times.Never);

            AuditServiceMock.Verify(
                a => a.LogAsync(
                    It.Is<CreateAuditLogDto>(dto =>
                        dto.Action == AuditAction.IssueCreated &&
                        dto.UserId == reporterId &&
                        dto.EntityName == nameof(Issue)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}