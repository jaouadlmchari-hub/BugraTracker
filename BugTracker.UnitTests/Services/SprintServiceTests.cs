using BugTracker.Application.DTOs.Sprints;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BugTracker.UnitTests.Services
{
    public class SprintServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ISprintRepository> _sprintRepositoryMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IIssueRepository> _issueRepositoryMock;
        private readonly SprintService _sut;

        public SprintServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _sprintRepositoryMock = new Mock<ISprintRepository>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _issueRepositoryMock = new Mock<IIssueRepository>();

            _unitOfWorkMock
                .SetupGet(u => u.Sprints)
                .Returns(_sprintRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Projects)
                .Returns(_projectRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Issues)
                .Returns(_issueRepositoryMock.Object);

            _sut = new SprintService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task StartAsync_WhenSprintIsPlanningAndNoActiveSprint_ShouldStartSprint()
        {
            // Arrange
            var sprintId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            _sprintRepositoryMock
                .Setup(r => r.GetActiveSprintsAsync(projectId))
                .ReturnsAsync(new List<Sprint>());

            // Act
            await _sut.StartAsync(sprintId);

            // Assert
            sprint.Status.Should().Be(SprintStatus.Active);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task StartAsync_WhenSprintIsNotPlanning_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = SprintStatus.Active
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => _sut.StartAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task StartAsync_WhenAnotherSprintIsAlreadyActive_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 2",
                Status = SprintStatus.Planning
            };

            var activeSprint = new Sprint
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Active
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            _sprintRepositoryMock
                .Setup(r => r.GetActiveSprintsAsync(projectId))
                .ReturnsAsync(new List<Sprint> { activeSprint });

            // Act
            Func<Task> act = () => _sut.StartAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            sprint.Status.Should().Be(SprintStatus.Planning);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task StartAsync_WhenSprintDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            Func<Task> act = () => _sut.StartAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _sprintRepositoryMock.Verify(
                r => r.GetActiveSprintsAsync(It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new CreateSprintDto
            {
                Name = "Sprint 1",
                Goal = "Finish authentication"
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _sprintRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Sprint>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEndDateIsBeforeStartDate_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG"
            };

            var startDate = DateTime.UtcNow.Date;

            var dto = new CreateSprintDto
            {
                Name = "Sprint 1",
                Goal = "Finish authentication",
                StartDate = startDate,
                EndDate = startDate.AddDays(-1)
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            Func<Task> act = () => _sut.CreateAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _sprintRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Sprint>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenDataIsValid_ShouldCreateSprintInPlanningStatus()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG"
            };

            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(14);

            var dto = new CreateSprintDto
            {
                Name = "Sprint 1",
                Goal = "Finish authentication",
                StartDate = startDate,
                EndDate = endDate
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            Sprint? createdSprint = null;

            _sprintRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Sprint>()))
                .Callback<Sprint>(s => createdSprint = s)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateAsync(projectId, dto);

            // Assert
            createdSprint.Should().NotBeNull();

            createdSprint!.ProjectId.Should().Be(projectId);
            createdSprint.Name.Should().Be(dto.Name);
            createdSprint.Goal.Should().Be(dto.Goal);
            createdSprint.StartDate.Should().Be(dto.StartDate);
            createdSprint.EndDate.Should().Be(dto.EndDate);
            createdSprint.Status.Should().Be(SprintStatus.Planning);

            result.ProjectId.Should().Be(projectId);
            result.Name.Should().Be(dto.Name);
            result.Status.Should().Be(SprintStatus.Planning);

            _sprintRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Sprint>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenSprintDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var dto = new UpdateSprintDto
            {
                Name = "Sprint Updated",
                Goal = "Updated goal"
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(sprintId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Theory]
        [InlineData(SprintStatus.Active)]
        [InlineData(SprintStatus.Completed)]
        public async Task UpdateAsync_WhenSprintIsNotPlanning_ShouldThrowBusinessRuleException(SprintStatus status)
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = status
            };

            var dto = new UpdateSprintDto
            {
                Name = "Sprint Updated",
                Goal = "Updated goal"
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(sprintId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenDatesAreInvalid_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.Date;

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            var dto = new UpdateSprintDto
            {
                Name = "Sprint Updated",
                Goal = "Updated goal",
                StartDate = startDate,
                EndDate = startDate.AddDays(-1)
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(sprintId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            sprint.Name.Should().Be("Sprint 1");

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenDataIsValid_ShouldUpdateSprint()
        {
            // Arrange
            var sprintId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Goal = "Old goal",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7),
                Status = SprintStatus.Planning
            };

            var newStartDate = DateTime.UtcNow.Date.AddDays(1);
            var newEndDate = newStartDate.AddDays(14);

            var dto = new UpdateSprintDto
            {
                Name = "Sprint Updated",
                Goal = "New goal",
                StartDate = newStartDate,
                EndDate = newEndDate
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            var result = await _sut.UpdateAsync(sprintId, dto);

            // Assert
            sprint.Name.Should().Be(dto.Name);
            sprint.Goal.Should().Be(dto.Goal);
            sprint.StartDate.Should().Be(dto.StartDate);
            sprint.EndDate.Should().Be(dto.EndDate);

            result.Name.Should().Be(dto.Name);
            result.Goal.Should().Be(dto.Goal);
            result.StartDate.Should().Be(dto.StartDate);
            result.EndDate.Should().Be(dto.EndDate);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task CompleteAsync_WhenSprintDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            Func<Task> act = () => _sut.CompleteAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Theory]
        [InlineData(SprintStatus.Planning)]
        [InlineData(SprintStatus.Completed)]
        public async Task CompleteAsync_WhenSprintIsNotActive_ShouldThrowBusinessRuleException(SprintStatus status)
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = status
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => _sut.CompleteAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_WhenSprintIsActive_ShouldCompleteSprintAndMoveUnfinishedIssuesToBacklog()
        {
            // Arrange
            var sprintId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = projectId,
                Name = "Sprint 1",
                Status = SprintStatus.Active
            };

            var issue1 = new Issue
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                SprintId = sprintId,
                Status = IssueStatus.Todo
            };

            var issue2 = new Issue
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                SprintId = sprintId,
                Status = IssueStatus.InReview
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            _issueRepositoryMock
                .Setup(r => r.GetUnfinishedBySprintIdAsync(sprintId))
                .ReturnsAsync(new List<Issue> { issue1, issue2 });

            // Act
            await _sut.CompleteAsync(sprintId);

            // Assert
            issue1.SprintId.Should().BeNull();
            issue2.SprintId.Should().BeNull();

            sprint.Status.Should().Be(SprintStatus.Completed);
            sprint.CompletedAt.Should().NotBeNull();

            _issueRepositoryMock.Verify(
                r => r.GetUnfinishedBySprintIdAsync(sprintId),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenSprintDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            Func<Task> act = () => _sut.DeleteAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _sprintRepositoryMock.Verify(
                r => r.Delete(It.IsAny<Sprint>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenSprintIsActive_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = SprintStatus.Active
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            Func<Task> act = () => _sut.DeleteAsync(sprintId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _sprintRepositoryMock.Verify(
                r => r.Delete(It.IsAny<Sprint>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Theory]
        [InlineData(SprintStatus.Planning)]
        [InlineData(SprintStatus.Completed)]
        public async Task DeleteAsync_WhenSprintIsNotActive_ShouldDeleteSprint(SprintStatus status)
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = status
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            await _sut.DeleteAsync(sprintId);

            // Assert
            _sprintRepositoryMock.Verify(
                r => r.Delete(sprint),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenSprintExists_ShouldReturnSprintDto()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            var sprint = new Sprint
            {
                Id = sprintId,
                ProjectId = Guid.NewGuid(),
                Name = "Sprint 1",
                Status = SprintStatus.Planning
            };

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync(sprint);

            // Act
            var result = await _sut.GetByIdAsync(sprintId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(sprintId);
            result.Name.Should().Be("Sprint 1");
            result.Status.Should().Be(SprintStatus.Planning);
        }

        [Fact]
        public async Task GetByIdAsync_WhenSprintDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var sprintId = Guid.NewGuid();

            _sprintRepositoryMock
                .Setup(r => r.GetByIdAsync(sprintId))
                .ReturnsAsync((Sprint?)null);

            // Act
            var result = await _sut.GetByIdAsync(sprintId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByProjectAsync_ShouldReturnProjectSprints()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var sprints = new List<Sprint>
                {
                    new Sprint
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "Sprint 1",
                        Status = SprintStatus.Completed
                    },
                
                    new Sprint
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "Sprint 2",
                        Status = SprintStatus.Planning
                    }
                };

            _sprintRepositoryMock
                .Setup(r => r.GetByProjectIdAsync(projectId))
                .ReturnsAsync(sprints);

            // Act
            var result = await _sut.GetAllByProjectAsync(projectId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(s => s.Name == "Sprint 1");
            result.Should().Contain(s => s.Name == "Sprint 2");

            _sprintRepositoryMock.Verify(
                r => r.GetByProjectIdAsync(projectId),
                Times.Once);
        }
    }
}