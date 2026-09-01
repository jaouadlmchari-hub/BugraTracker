using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BugTracker.UnitTests.Services
{
    public class ProjectServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly ProjectService _sut;

        public ProjectServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _unitOfWorkMock
                .SetupGet(u => u.Projects)
                .Returns(_projectRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Users)
                .Returns(_userRepositoryMock.Object);

            _sut = new ProjectService(
                _unitOfWorkMock.Object,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task CreateAsync_WhenProjectKeyAlreadyExists_ShouldThrowConflictException()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var dto = new CreateProjectDto
            {
                Name = "BugTracker",
                Key = "BUG",
                Description = "Project description"
            };

            var existingProject = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Existing Project",
                Key = "BUG"
            };

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(ownerId);

            _projectRepositoryMock
                .Setup(r => r.GetByKeyAsync(dto.Key))
                .ReturnsAsync(existingProject);

            // Act
            Func<Task> act = () => _sut.CreateAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<ConflictException>();

            _projectRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Project>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenDataIsValid_ShouldCreateProjectWithCurrentUserAsOwnerAndManager()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var dto = new CreateProjectDto
            {
                Name = "BugTracker",
                Key = "BUG",
                Description = "Bug tracking application"
            };

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(ownerId);

            _projectRepositoryMock
                .Setup(r => r.GetByKeyAsync(dto.Key))
                .ReturnsAsync((Project?)null);

            Project? createdProject = null;

            _projectRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Project>()))
                .Callback<Project>(p => createdProject = p)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            createdProject.Should().NotBeNull();

            createdProject!.Name.Should().Be(dto.Name);
            createdProject.Key.Should().Be(dto.Key);
            createdProject.Description.Should().Be(dto.Description);
            createdProject.OwnerId.Should().Be(ownerId);
            createdProject.Status.Should().Be(ProjectStatus.Active);

            createdProject.Members.Should().ContainSingle();

            var ownerMember = createdProject.Members.Single();

            ownerMember.UserId.Should().Be(ownerId);
            ownerMember.Role.Should().Be(ProjectRole.Manager);

            result.Name.Should().Be(dto.Name);
            result.Key.Should().Be(dto.Key);

            _projectRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Project>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new UpdateProjectDto
            {
                Name = "Updated Project",
                Description = "Updated description"
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenProjectExists_ShouldUpdateProject()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "Old Project",
                Description = "Old description",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var dto = new UpdateProjectDto
            {
                Name = "Updated Project",
                Description = "Updated description"
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

             var beforeUpdate = DateTime.UtcNow;

            // Act
            var result = await _sut.UpdateAsync(projectId, dto);

            // Assert
            project.Name.Should().Be(dto.Name);
            project.Description.Should().Be(dto.Description);
            project.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);

            result.Name.Should().Be(dto.Name);
            result.Description.Should().Be(dto.Description);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ArchiveAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.ArchiveAsync(projectId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ArchiveAsync_WhenProjectIsAlreadyArchived_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Archived
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            Func<Task> act = () => _sut.ArchiveAsync(projectId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            project.Status.Should().Be(ProjectStatus.Archived);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ArchiveAsync_WhenProjectIsActive_ShouldArchiveProject()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            var beforeArchive = DateTime.UtcNow;

            // Act
            await _sut.ArchiveAsync(projectId);

            // Assert
            project.Status.Should().Be(ProjectStatus.Archived);
            project.UpdatedAt.Should().BeOnOrAfter(beforeArchive);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ActivateAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.ActivateAsync(projectId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ActivateAsync_WhenProjectIsAlreadyActive_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            Func<Task> act = () => _sut.ActivateAsync(projectId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            project.Status.Should().Be(ProjectStatus.Active);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ActivateAsync_WhenProjectIsArchived_ShouldActivateProject()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Archived
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            var beforeActivation = DateTime.UtcNow;

            // Act
            await _sut.ActivateAsync(projectId);

            // Assert
            project.Status.Should().Be(ProjectStatus.Active);
            project.UpdatedAt.Should().BeOnOrAfter(beforeActivation);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ChangeOwnerAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.ChangeOwnerAsync(projectId, newOwnerId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _userRepositoryMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeOwnerAsync_WhenNewOwnerDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active,
                OwnerId = Guid.NewGuid()
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(newOwnerId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.ChangeOwnerAsync(projectId, newOwnerId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            project.OwnerId.Should().NotBe(newOwnerId);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeOwnerAsync_WhenNewOwnerIsInactive_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var currentOwnerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active,
                OwnerId = currentOwnerId
            };

            var newOwner = new User
            {
                Id = newOwnerId,
                Email = "newowner@test.com",
                Username = "newowner",
                IsActive = false
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(newOwnerId))
                .ReturnsAsync(newOwner);

            // Act
            Func<Task> act = () => _sut.ChangeOwnerAsync(projectId, newOwnerId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            project.OwnerId.Should().Be(currentOwnerId);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeOwnerAsync_WhenUserIsAlreadyOwner_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active,
                OwnerId = ownerId
            };

            var owner = new User
            {
                Id = ownerId,
                Email = "owner@test.com",
                Username = "owner",
                IsActive = true
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(ownerId))
                .ReturnsAsync(owner);

            // Act
            Func<Task> act = () => _sut.ChangeOwnerAsync(projectId, ownerId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            project.OwnerId.Should().Be(ownerId);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeOwnerAsync_WhenDataIsValid_ShouldChangeOwner()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var currentOwnerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active,
                OwnerId = currentOwnerId
            };

            var newOwner = new User
            {
                Id = newOwnerId,
                Email = "newowner@test.com",
                Username = "newowner",
                IsActive = true
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(newOwnerId))
                .ReturnsAsync(newOwner);

            var beforeChange = DateTime.UtcNow;

            // Act
            await _sut.ChangeOwnerAsync(projectId, newOwnerId);

            // Assert
            project.OwnerId.Should().Be(newOwnerId);
            project.UpdatedAt.Should().BeOnOrAfter(beforeChange);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.DeleteAsync(projectId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectRepositoryMock.Verify(
                r => r.Delete(It.IsAny<Project>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenProjectExists_ShouldDeleteProject()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            await _sut.DeleteAsync(projectId);

            // Assert
            _projectRepositoryMock.Verify(
                r => r.Delete(project),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProjectExists_ShouldReturnProjectDto()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Description = "Bug tracking application",
                Status = ProjectStatus.Active
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            var result = await _sut.GetByIdAsync(projectId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(projectId);
            result.Name.Should().Be("BugTracker");
            result.Key.Should().Be("BUG");
            result.Status.Should().Be(ProjectStatus.Active);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProjectDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            var result = await _sut.GetByIdAsync(projectId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByKeyAsync_WhenProjectExists_ShouldNormalizeKeyAndReturnProjectDto()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            _projectRepositoryMock
                .Setup(r => r.GetByKeyAsync("BUG"))
                .ReturnsAsync(project);

            // Act
            var result = await _sut.GetByKeyAsync("  bug  ");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(projectId);
            result.Key.Should().Be("BUG");

            _projectRepositoryMock.Verify(
                r => r.GetByKeyAsync("BUG"),
                Times.Once);
        }

        [Fact]
        public async Task GetByKeyAsync_WhenProjectDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            _projectRepositoryMock
                .Setup(r => r.GetByKeyAsync("UNKNOWN"))
                .ReturnsAsync((Project?)null);

            // Act
            var result = await _sut.GetByKeyAsync("unknown");

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAllPaginatedAsync_ShouldReturnPagedProjectsForCurrentUser(bool isAdmin)
        {
            // Arrange
            var userId = Guid.NewGuid();

            var filter = new ProjectFilterDto
            {
                PageNumber = 2,
                PageSize = 10
            };

            var projects = new List<Project>
                {
                    new Project
                    {
                        Id = Guid.NewGuid(),
                        Name = "BugTracker",
                        Key = "BUG",
                        Status = ProjectStatus.Active
                    },
                    new Project
                    {
                        Id = Guid.NewGuid(),
                        Name = "LOMS",
                        Key = "LOMS",
                        Status = ProjectStatus.Active
                    }
                };

            const int totalCount = 25;

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(userId);

            _currentUserServiceMock
                .SetupGet(c => c.IsAdmin)
                .Returns(isAdmin);

            _projectRepositoryMock
                .Setup(r => r.GetPaginatedAsync(filter, userId, isAdmin))
                .ReturnsAsync((projects, totalCount));

            // Act
            var result = await _sut.GetAllPaginatedAsync(filter);

            // Assert
            result.Items.Should().HaveCount(2);

            result.Items.Should().Contain(p => p.Key == "BUG");
            result.Items.Should().Contain(p => p.Key == "LOMS");

            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);

            _projectRepositoryMock.Verify(
                r => r.GetPaginatedAsync(filter, userId, isAdmin),
                Times.Once);
        }

    }
}