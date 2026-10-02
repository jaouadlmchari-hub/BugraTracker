using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BugTracker.UnitTests.Services
{
    public class EpicServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEpicRepository> _epicRepositoryMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<EpicService>> _loggerMock;
        private readonly EpicService _sut;

        public EpicServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _epicRepositoryMock = new Mock<IEpicRepository>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<EpicService>>();

            _unitOfWorkMock
                .SetupGet(u => u.Epics)
                .Returns(_epicRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Projects)
                .Returns(_projectRepositoryMock.Object);

            _sut = new EpicService(
                _unitOfWorkMock.Object,
                _currentUserServiceMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        // ==================== CREATE ====================

        [Fact]
        public async Task CreateAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var dto = new CreateEpicDto
            {
                Title = "Authentication",
                Description = "Authentication and authorization",
                ColorCode = "#3B82F6"
            };

            _projectRepositoryMock
                .Setup(r => r.ExistsAsync(projectId))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = () => _sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _epicRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Epic>()), Times.Never);
            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenProjectExists_ShouldCreateEpicWithActiveStatus()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            const string currentUserEmail = "user@test.com";

            var dto = new CreateEpicDto
            {
                Title = "Authentication",
                Description = "Authentication and authorization",
                ColorCode = "#3B82F6"
            };

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _currentUserServiceMock
                .SetupGet(c => c.Email)
                .Returns(currentUserEmail);

            _projectRepositoryMock
                .Setup(r => r.ExistsAsync(projectId))
                .ReturnsAsync(true);

            Epic? createdEpic = null;

            _epicRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Epic>()))
                .Callback<Epic>(e => createdEpic = e)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateAsync(projectId, dto);

            // Assert
            createdEpic.Should().NotBeNull();
            createdEpic!.ProjectId.Should().Be(projectId);
            createdEpic.Title.Should().Be(dto.Title);
            createdEpic.Description.Should().Be(dto.Description);
            createdEpic.ColorCode.Should().Be(dto.ColorCode);
            createdEpic.Status.Should().Be(EpicStatus.Active);

            result.ProjectId.Should().Be(projectId);
            result.Title.Should().Be(dto.Title);
            result.Status.Should().Be(EpicStatus.Active);

            _epicRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Epic>()), Times.Once);

            _auditServiceMock.Verify(a => a.LogAsync(
                It.Is<CreateAuditLogDto>(d =>
                    d.UserId == currentUserId &&
                    d.UserEmail == currentUserEmail &&
                    d.Action == AuditAction.EpicCreated &&
                    d.EntityName == nameof(Epic) &&
                    d.EntityId == createdEpic.Id.ToString() &&
                    d.Details != null &&
                    d.Details.Contains("Création de l'Epic")),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==================== UPDATE ====================

        [Fact]
        public async Task UpdateAsync_WhenEpicDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var dto = new UpdateEpicDto
            {
                Title = "Updated Epic",
                Description = "Updated description",
                ColorCode = "#FF5733"
            };

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(epicId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenEpicIsArchived_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var epic = new Epic
            {
                Id = epicId,
                ProjectId = Guid.NewGuid(),
                Title = "Old Epic",
                Status = EpicStatus.Archived
            };

            var dto = new UpdateEpicDto
            {
                Title = "Updated Epic",
                Description = "Updated description",
                ColorCode = "#FF5733"
            };

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(epicId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            epic.Title.Should().Be("Old Epic");

            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenEpicIsActive_ShouldUpdateEpic()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            const string currentUserEmail = "user@test.com";

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = projectId,
                Title = "Old Epic",
                Description = "Old description",
                ColorCode = "#3B82F6",
                Status = EpicStatus.Active
            };

            var dto = new UpdateEpicDto
            {
                Title = "Updated Epic",
                Description = "Updated description",
                ColorCode = "#FF5733"
            };

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _currentUserServiceMock
                .SetupGet(c => c.Email)
                .Returns(currentUserEmail);

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            var result = await _sut.UpdateAsync(epicId, dto);

            // Assert
            epic.Title.Should().Be(dto.Title);
            epic.Description.Should().Be(dto.Description);
            epic.ColorCode.Should().Be(dto.ColorCode);

            result.Title.Should().Be(dto.Title);
            result.Description.Should().Be(dto.Description);
            result.ColorCode.Should().Be(dto.ColorCode);

            _auditServiceMock.Verify(a => a.LogAsync(
                It.Is<CreateAuditLogDto>(d =>
                    d.UserId == currentUserId &&
                    d.UserEmail == currentUserEmail &&
                    d.Action == AuditAction.EpicUpdated &&
                    d.EntityName == nameof(Epic) &&
                    d.EntityId == epicId.ToString() &&
                    d.Details != null &&
                    d.Details.Contains("Mise à jour de l'Epic")),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==================== CHANGE STATUS ====================

        [Fact]
        public async Task ChangeStatusAsync_WhenEpicDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            Func<Task> act = () => _sut.ChangeStatusAsync(epicId, EpicStatus.Archived);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangeStatusAsync_WhenStatusIsInvalid_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var epic = new Epic
            {
                Id = epicId,
                ProjectId = Guid.NewGuid(),
                Title = "Epic 1",
                Status = EpicStatus.Active
            };

            var invalidStatus = (EpicStatus)999;

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            Func<Task> act = () => _sut.ChangeStatusAsync(epicId, invalidStatus);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            epic.Status.Should().Be(EpicStatus.Active);

            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangeStatusAsync_WhenStatusIsValid_ShouldChangeStatus()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            const string currentUserEmail = "user@test.com";

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = Guid.NewGuid(),
                Title = "Epic 1",
                Status = EpicStatus.Active
            };

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _currentUserServiceMock
                .SetupGet(c => c.Email)
                .Returns(currentUserEmail);

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            await _sut.ChangeStatusAsync(epicId, EpicStatus.Archived);

            // Assert
            epic.Status.Should().Be(EpicStatus.Archived);

            // Note : dans le service, Action = AuditAction.EpicUpdated pour le changement de statut
            _auditServiceMock.Verify(a => a.LogAsync(
                It.Is<CreateAuditLogDto>(d =>
                    d.UserId == currentUserId &&
                    d.UserEmail == currentUserEmail &&
                    d.Action == AuditAction.EpicUpdated &&
                    d.EntityName == nameof(Epic) &&
                    d.EntityId == epicId.ToString() &&
                    d.Details != null &&
                    d.Details.Contains("Changement du statut")),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==================== DELETE ====================

        [Fact]
        public async Task DeleteAsync_WhenEpicDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            _epicRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            Func<Task> act = () => _sut.DeleteAsync(epicId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _epicRepositoryMock.Verify(r => r.Delete(It.IsAny<Epic>()), Times.Never);
            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<CreateAuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenEpicExists_ShouldDetachIssuesAndDeleteEpic()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            const string currentUserEmail = "user@test.com";

            var issue1 = new Issue
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EpicId = epicId
            };

            var issue2 = new Issue
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EpicId = epicId
            };

            var epic = new Epic
            {
                Id = epicId,
                ProjectId = projectId,
                Title = "Epic 1",
                Status = EpicStatus.Active,
                Issues = new List<Issue> { issue1, issue2 }
            };

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _currentUserServiceMock
                .SetupGet(c => c.Email)
                .Returns(currentUserEmail);

            _epicRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            await _sut.DeleteAsync(epicId);

            // Assert
            issue1.EpicId.Should().BeNull();
            issue2.EpicId.Should().BeNull();

            _epicRepositoryMock.Verify(r => r.Delete(epic), Times.Once);

            _auditServiceMock.Verify(a => a.LogAsync(
                It.Is<CreateAuditLogDto>(d =>
                    d.UserId == currentUserId &&
                    d.UserEmail == currentUserEmail &&
                    d.Action == AuditAction.EpicDeleted &&
                    d.EntityName == nameof(Epic) &&
                    d.EntityId == epicId.ToString() &&
                    d.Details != null &&
                    d.Details.Contains("Suppression de l'Epic")),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==================== GETTERS ====================

        [Fact]
        public async Task GetByIdAsync_WhenEpicExists_ShouldReturnEpicDto()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var epic = new Epic
            {
                Id = epicId,
                ProjectId = Guid.NewGuid(),
                Title = "Epic 1",
                Status = EpicStatus.Active
            };

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            var result = await _sut.GetByIdAsync(epicId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(epicId);
            result.Title.Should().Be("Epic 1");
            result.Status.Should().Be(EpicStatus.Active);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEpicDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            _epicRepositoryMock
                .Setup(r => r.GetByIdAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            var result = await _sut.GetByIdAsync(epicId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_WhenEpicExists_ShouldReturnDetails()
        {
            // Arrange
            var epicId = Guid.NewGuid();
            var epic = new Epic
            {
                Id = epicId,
                ProjectId = Guid.NewGuid(),
                Title = "Epic 1",
                Status = EpicStatus.Active,
                Issues = new List<Issue>()
            };

            _epicRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(epicId))
                .ReturnsAsync(epic);

            // Act
            var result = await _sut.GetByIdWithDetailsAsync(epicId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(epicId);
            result.Title.Should().Be("Epic 1");
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_WhenEpicDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var epicId = Guid.NewGuid();

            _epicRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(epicId))
                .ReturnsAsync((Epic?)null);

            // Act
            var result = await _sut.GetByIdWithDetailsAsync(epicId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByProjectAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.GetAllByProjectAsync(projectId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _epicRepositoryMock.Verify(r => r.GetByProjectIdAsync(projectId), Times.Never);
        }

        [Fact]
        public async Task GetAllByProjectAsync_WhenProjectExists_ShouldReturnEpics()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG"
            };

            var epics = new List<Epic>
            {
                new Epic
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Title = "Epic 1",
                    Status = EpicStatus.Active
                },
                new Epic
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Title = "Epic 2",
                    Status = EpicStatus.Archived
                }
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _epicRepositoryMock
                .Setup(r => r.GetByProjectIdAsync(projectId))
                .ReturnsAsync(epics);

            // Act
            var result = await _sut.GetAllByProjectAsync(projectId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(e => e.Title == "Epic 1");
            result.Should().Contain(e => e.Title == "Epic 2");
        }

        [Fact]
        public async Task GetActiveByProjectAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.GetActiveByProjectAsync(projectId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _epicRepositoryMock.Verify(r => r.GetActiveEpicsAsync(projectId), Times.Never);
        }

        [Fact]
        public async Task GetActiveByProjectAsync_WhenProjectExists_ShouldReturnActiveEpics()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG"
            };

            var activeEpics = new List<Epic>
            {
                new Epic
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Title = "Authentication",
                    Status = EpicStatus.Active
                },
                new Epic
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Title = "Issue Management",
                    Status = EpicStatus.Active
                }
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _epicRepositoryMock
                .Setup(r => r.GetActiveEpicsAsync(projectId))
                .ReturnsAsync(activeEpics);

            // Act
            var result = await _sut.GetActiveByProjectAsync(projectId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(e => e.Status == EpicStatus.Active);

            _epicRepositoryMock.Verify(r => r.GetActiveEpicsAsync(projectId), Times.Once);
        }
    }
}