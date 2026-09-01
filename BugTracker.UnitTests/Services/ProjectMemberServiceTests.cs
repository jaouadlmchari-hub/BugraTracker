using BugTracker.Application.DTOs.ProjectMembers;
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
    public class ProjectMemberServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IProjectMemberRepository> _projectMemberRepositoryMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IIssueRepository> _issueRepositoryMock;
        private readonly ProjectMemberService _sut;

        public ProjectMemberServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _projectMemberRepositoryMock = new Mock<IProjectMemberRepository>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _issueRepositoryMock = new Mock<IIssueRepository>();

            _unitOfWorkMock
                .SetupGet(u => u.ProjectMembers)
                .Returns(_projectMemberRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Projects)
                .Returns(_projectRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Users)
                .Returns(_userRepositoryMock.Object);

            _unitOfWorkMock
                .SetupGet(u => u.Issues)
                .Returns(_issueRepositoryMock.Object);

            _sut = new ProjectMemberService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task AddMemberAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            var dto = new AddProjectMemberDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.AddMemberAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _userRepositoryMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AddMemberAsync_WhenProjectIsArchived_ShouldThrowBusinessRuleException()
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

            var dto = new AddProjectMemberDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            Func<Task> act = () => _sut.AddMemberAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _userRepositoryMock.Verify(
                r => r.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AddMemberAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var dto = new AddProjectMemberDto
            {
                UserId = userId,
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.AddMemberAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(
                    projectId,
                    userId),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AddMemberAsync_WhenUserIsInactive_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var user = new User
            {
                Id = userId,
                Email = "user@test.com",
                Username = "user1",
                IsActive = false
            };

            var dto = new AddProjectMemberDto
            {
                UserId = userId,
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = () => _sut.AddMemberAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(projectId, userId),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AddMemberAsync_WhenUserIsAlreadyMember_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var user = new User
            {
                Id = userId,
                Email = "user@test.com",
                Username = "user1",
                IsActive = true
            };

            var existingMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Developer
            };

            var dto = new AddProjectMemberDto
            {
                UserId = userId,
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(existingMember);

            // Act
            Func<Task> act = () => _sut.AddMemberAsync(projectId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _projectMemberRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AddMemberAsync_WhenDataIsValid_ShouldAddMember()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var user = new User
            {
                Id = userId,
                Email = "user@test.com",
                Username = "user1",
                FullName = "User One",
                IsActive = true
            };

            var dto = new AddProjectMemberDto
            {
                UserId = userId,
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync((ProjectMember?)null);

            ProjectMember? createdMember = null;

            _projectMemberRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ProjectMember>()))
                .Callback<ProjectMember>(m => createdMember = m)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.AddMemberAsync(projectId, dto);

            // Assert
            createdMember.Should().NotBeNull();

            createdMember!.ProjectId.Should().Be(projectId);
            createdMember.UserId.Should().Be(userId);
            createdMember.Role.Should().Be(ProjectRole.Developer);

            result.UserId.Should().Be(userId);
            result.Username.Should().Be("user1");
            result.FullName.Should().Be("User One");
            result.Role.Should().Be(ProjectRole.Developer);

            _projectMemberRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<ProjectMember>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ChangeRoleAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.ChangeRoleAsync(projectId, userId, ProjectRole.QA);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeRoleAsync_WhenProjectIsArchived_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

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
            Func<Task> act = () => _sut.ChangeRoleAsync(projectId, userId, ProjectRole.QA);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeRoleAsync_WhenMemberDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

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

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync((ProjectMember?)null);

            // Act
            Func<Task> act = () => _sut.ChangeRoleAsync(projectId, userId, ProjectRole.QA);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectMemberRepositoryMock.Verify(
                r => r.CountManagersAsync(It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeRoleAsync_WhenMemberAlreadyHasRole_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Developer
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            // Act
            Func<Task> act = () => _sut.ChangeRoleAsync(projectId, userId, ProjectRole.Developer);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _projectMemberRepositoryMock.Verify(
                r => r.CountManagersAsync(It.IsAny<Guid>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Theory]
        [InlineData(ProjectRole.Developer)]
        [InlineData(ProjectRole.QA)]
        public async Task ChangeRoleAsync_WhenDemotingLastManager_ShouldThrowBusinessRuleException(ProjectRole newRole)
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Manager
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            _projectMemberRepositoryMock
                .Setup(r => r.CountManagersAsync(projectId))
                .ReturnsAsync(1);

            // Act
            Func<Task> act = () => _sut.ChangeRoleAsync(projectId, userId, newRole);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            member.Role.Should().Be(ProjectRole.Manager);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeRoleAsync_WhenAnotherManagerExists_ShouldChangeRole()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Manager
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            _projectMemberRepositoryMock
                .Setup(r => r.CountManagersAsync(projectId))
                .ReturnsAsync(2);

            // Act
            await _sut.ChangeRoleAsync(projectId, userId, ProjectRole.Developer);

            // Assert
            member.Role.Should().Be(ProjectRole.Developer);

            _projectMemberRepositoryMock.Verify(
                r => r.CountManagersAsync(projectId),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task RemoveMemberAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.RemoveMemberAsync(projectId, userId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.Delete(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_WhenProjectIsArchived_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

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
            Func<Task> act = () => _sut.RemoveMemberAsync(projectId, userId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.Delete(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_WhenMemberDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

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

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync((ProjectMember?)null);

            // Act
            Func<Task> act = () => _sut.RemoveMemberAsync(projectId, userId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectMemberRepositoryMock.Verify(
                r => r.CountManagersAsync(It.IsAny<Guid>()),
                Times.Never);

            _issueRepositoryMock.Verify(
                r => r.GetByProjectAndAssigneeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.Delete(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_WhenMemberIsProjectOwner_ShouldThrowBusinessRuleException()
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

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = ownerId,
                Role = ProjectRole.Manager
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, ownerId))
                .ReturnsAsync(member);

            // Act
            Func<Task> act = () => _sut.RemoveMemberAsync(projectId, ownerId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _projectMemberRepositoryMock.Verify(
                r => r.CountManagersAsync(It.IsAny<Guid>()),
                Times.Never);

            _issueRepositoryMock.Verify(
                r => r.GetByProjectAndAssigneeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.Delete(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_WhenMemberIsLastManager_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active,
                OwnerId = Guid.NewGuid()
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Manager
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            _projectMemberRepositoryMock
                .Setup(r => r.CountManagersAsync(projectId))
                .ReturnsAsync(1);

            // Act
            Func<Task> act = () => _sut.RemoveMemberAsync(projectId, userId);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>();

            _issueRepositoryMock.Verify(
                r => r.GetByProjectAndAssigneeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);

            _projectMemberRepositoryMock.Verify(
                r => r.Delete(It.IsAny<ProjectMember>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_WhenRemovalIsValid_ShouldUnassignIssuesAndRemoveMember()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var project = new Project
            {
                Id = projectId,
                Name = "BugTracker",
                Key = "BUG",
                Status = ProjectStatus.Active,
                OwnerId = Guid.NewGuid()
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Developer
            };

            var issue1 = new Issue
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                AssigneeId = userId
            };

            var issue2 = new Issue
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                AssigneeId = userId
            };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            _issueRepositoryMock
                .Setup(r => r.GetByProjectAndAssigneeAsync(projectId, userId))
                .ReturnsAsync(new List<Issue> { issue1, issue2 });

            // Act
            await _sut.RemoveMemberAsync(projectId, userId);

            // Assert
            issue1.AssigneeId.Should().BeNull();
            issue2.AssigneeId.Should().BeNull();

            _projectMemberRepositoryMock.Verify(
                r => r.Delete(member),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetMembersAsync_WhenProjectDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = () => _sut.GetMembersAsync(projectId);

            // Assert
            await act.Should()
                .ThrowAsync<NotFoundException>();

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetMembersAsync_WhenProjectExists_ShouldReturnMembers()
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

            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Username = "dev1",
                FullName = "Developer One"
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Username = "qa1",
                FullName = "QA One"
            };

            var members = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        ProjectId = projectId,
                        UserId = user1.Id,
                        Role = ProjectRole.Developer,
                        User = user1
                    },
                    new ProjectMember
                    {
                        ProjectId = projectId,
                        UserId = user2.Id,
                        Role = ProjectRole.QA,
                        User = user2
                    }
                };

            _projectRepositoryMock
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectIdAsync(projectId))
                .ReturnsAsync(members);

            // Act
            var result = await _sut.GetMembersAsync(projectId);

            // Assert
            result.Should().HaveCount(2);

            result.Should().Contain(m => m.UserId == user1.Id && m.Role == ProjectRole.Developer);
            result.Should().Contain(m => m.UserId == user2.Id && m.Role == ProjectRole.QA);

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectIdAsync(projectId),
                Times.Once);
        }

        [Fact]
        public async Task GetMemberAsync_WhenMemberExists_ShouldReturnMemberDto()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "dev1",
                FullName = "Developer One"
            };

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = ProjectRole.Developer,
                User = user
            };

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync(member);

            // Act
            var result = await _sut.GetMemberAsync(projectId, userId);

            // Assert
            result.Should().NotBeNull();

            result!.UserId.Should().Be(userId);
            result.Username.Should().Be("dev1");
            result.FullName.Should().Be("Developer One");
            result.Role.Should().Be(ProjectRole.Developer);

            _projectMemberRepositoryMock.Verify(
                r => r.GetByProjectAndUserAsync(projectId, userId),
                Times.Once);
        }

        [Fact]
        public async Task GetMemberAsync_WhenMemberDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _projectMemberRepositoryMock
                .Setup(r => r.GetByProjectAndUserAsync(projectId, userId))
                .ReturnsAsync((ProjectMember?)null);

            // Act
            var result = await _sut.GetMemberAsync(projectId, userId);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShareAnyProjectAsync_ShouldReturnRepositoryResult(bool expectedResult)
        {
            // Arrange
            var firstUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();

            _projectMemberRepositoryMock
                .Setup(r => r.ShareAnyProjectAsync(firstUserId, secondUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _sut.ShareAnyProjectAsync(firstUserId, secondUserId);

            // Assert
            result.Should().Be(expectedResult);

            _projectMemberRepositoryMock.Verify(
                r => r.ShareAnyProjectAsync(firstUserId, secondUserId),
                Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsMemberAsync_ShouldReturnRepositoryResult(bool expectedResult)
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _projectMemberRepositoryMock
                .Setup(r => r.IsMemberAsync(projectId, userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _sut.IsMemberAsync(projectId, userId);

            // Assert
            result.Should().Be(expectedResult);

            _projectMemberRepositoryMock.Verify(
                r => r.IsMemberAsync(projectId, userId),
                Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsManagerAsync_ShouldReturnRepositoryResult(bool expectedResult)
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _projectMemberRepositoryMock
                .Setup(r => r.IsManagerAsync(projectId, userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _sut.IsManagerAsync(projectId, userId);

            // Assert
            result.Should().Be(expectedResult);

            _projectMemberRepositoryMock.Verify(
                r => r.IsManagerAsync(projectId, userId),
                Times.Once);
        }

    }
}