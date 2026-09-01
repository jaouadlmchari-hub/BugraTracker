using BugTracker.Application.DTOs.Users;
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
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();

            _unitOfWorkMock
                .SetupGet(u => u.Users)
                .Returns(_userRepositoryMock.Object);

            _sut = new UserService(
                _unitOfWorkMock.Object,
                _passwordHasherMock.Object);
        }

        [Fact]
        public async Task CreateAsync_WhenEmailAlreadyExists_ShouldThrowConflictException()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Username = "jaouad",
                Email = "jaouad@test.com",
                Password = "Password123!"
            };

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "existing",
                Email = dto.Email,
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(existingUser);

            // Act
            Func<Task> act = () => _sut.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();

            _passwordHasherMock.Verify(
                h => h.Hash(It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<User>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenDataIsValid_ShouldCreateDeveloperUser()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Username = "jaouad",
                Email = "jaouad@test.com",
                Password = "Password123!"
            };

            const string passwordHash = "hashed-password";

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(h => h.Hash(dto.Password))
                .Returns(passwordHash);

            User? createdUser = null;

            _userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(user => createdUser = user)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            createdUser.Should().NotBeNull();

            createdUser!.Username.Should().Be(dto.Username);
            createdUser.Email.Should().Be(dto.Email);
            createdUser.PasswordHash.Should().Be(passwordHash);
            createdUser.SystemRole.Should().Be(SystemRole.Developer);
            createdUser.IsActive.Should().BeTrue();

            result.Username.Should().Be(dto.Username);
            result.Email.Should().Be(dto.Email);
            result.SystemRole.Should().Be(SystemRole.Developer);

            _passwordHasherMock.Verify(h => h.Hash(dto.Password), Times.Once);
            _userRepositoryMock.Verify(r => r.AddAsync(createdUser), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AdminCreateAsync_WhenEmailAlreadyExists_ShouldThrowConflictException()
        {
            // Arrange
            var dto = new AdminCreateUserDto
            {
                Username = "admin2",
                Email = "admin@test.com",
                Password = "Password123!",
                SystemRole = SystemRole.Admin
            };

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "existing",
                Email = dto.Email,
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(existingUser);

            // Act
            Func<Task> act = () => _sut.AdminCreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();

            _passwordHasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);

            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AdminCreateAsync_WhenDataIsValid_ShouldCreateUserWithRequestedRole()
        {
            // Arrange
            var dto = new AdminCreateUserDto
            {
                Username = "admin2",
                Email = "admin2@test.com",
                Password = "Password123!",
                SystemRole = SystemRole.Admin
            };

            const string passwordHash = "hashed-password";

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(h => h.Hash(dto.Password))
                .Returns(passwordHash);

            User? createdUser = null;

            _userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(user => createdUser = user)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.AdminCreateAsync(dto);

            // Assert
            createdUser.Should().NotBeNull();

            createdUser!.Username.Should().Be(dto.Username);
            createdUser.Email.Should().Be(dto.Email);
            createdUser.PasswordHash.Should().Be(passwordHash);
            createdUser.SystemRole.Should().Be(SystemRole.Admin);
            createdUser.IsActive.Should().BeTrue();

            result.Username.Should().Be(dto.Username);
            result.Email.Should().Be(dto.Email);
            result.SystemRole.Should().Be(SystemRole.Admin);

            _passwordHasherMock.Verify(h => h.Hash(dto.Password), Times.Once);
            _userRepositoryMock.Verify(r => r.AddAsync(createdUser), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var dto = new UpdateUserDto
            {
                Username = "jaouad.updated",
                Email = "updated@test.com"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailIsUsedByAnotherUser_ShouldThrowConflictException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var anotherUserId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "old@test.com",
                IsActive = true
            };

            var dto = new UpdateUserDto
            {
                Username = "jaouad.updated",
                Email = "existing@test.com"
            };

            var userWithSameEmail = new User
            {
                Id = anotherUserId,
                Username = "anotheruser",
                Email = dto.Email,
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync(userWithSameEmail);

            // Act
            Func<Task> act = () => _sut.UpdateAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();

            user.Username.Should().Be("jaouad");
            user.Email.Should().Be("old@test.com");

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenDataIsValid_ShouldUpdateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "old@test.com",
                IsActive = true
            };

            var dto = new UpdateUserDto
            {
                Username = "jaouad.updated",
                Email = "updated@test.com"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(dto.Email))
                .ReturnsAsync((User?)null);

            var beforeUpdate = DateTime.UtcNow;

            // Act
            var result = await _sut.UpdateAsync(userId, dto);

            // Assert
            user.Username.Should().Be(dto.Username);
            user.Email.Should().Be(dto.Email);
            user.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);

            result.Username.Should().Be(dto.Username);
            result.Email.Should().Be(dto.Email);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeactivateAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.DeactivateAsync(userId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeactivateAsync_WhenUserExists_ShouldDeactivateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var beforeDeactivation = DateTime.UtcNow;

            // Act
            await _sut.DeactivateAsync(userId);

            // Assert
            user.IsActive.Should().BeFalse();
            user.UpdatedAt.Should().BeOnOrAfter(beforeDeactivation);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ActivateAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.ActivateAsync(userId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ActivateAsync_WhenUserExists_ShouldActivateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                IsActive = false
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var beforeActivation = DateTime.UtcNow;

            // Act
            await _sut.ActivateAsync(userId);

            // Assert
            user.IsActive.Should().BeTrue();
            user.UpdatedAt.Should().BeOnOrAfter(beforeActivation);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangeSystemRoleAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.ChangeSystemRoleAsync(userId, SystemRole.Admin);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangeSystemRoleAsync_WhenRoleIsInvalid_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                SystemRole = SystemRole.Developer,
                IsActive = true
            };

            var invalidRole = (SystemRole)999;

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = () => _sut.ChangeSystemRoleAsync(userId, invalidRole);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            user.SystemRole.Should().Be(SystemRole.Developer);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangeSystemRoleAsync_WhenRoleIsValid_ShouldChangeUserRole()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                SystemRole = SystemRole.Developer,
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var beforeChange = DateTime.UtcNow;

            // Act
            await _sut.ChangeSystemRoleAsync(userId, SystemRole.Admin);

            // Assert
            user.SystemRole.Should().Be(SystemRole.Admin);
            user.UpdatedAt.Should().BeOnOrAfter(beforeChange);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.ChangePasswordAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _passwordHasherMock.Verify(
                h => h.Verify(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            _passwordHasherMock.Verify(
                h => h.Hash(It.IsAny<string>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenCurrentPasswordIsIncorrect_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                PasswordHash = "old-password-hash",
                IsActive = true
            };

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewPassword123!"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Verify(dto.CurrentPassword, user.PasswordHash))
                .Returns(false);

            // Act
            Func<Task> act = () => _sut.ChangePasswordAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            user.PasswordHash.Should().Be("old-password-hash");

            _passwordHasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenNewPasswordIsSameAsCurrentPassword_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                PasswordHash = "old-password-hash",
                IsActive = true
            };

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Password123!",
                NewPassword = "Password123!"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Verify(dto.CurrentPassword, user.PasswordHash))
                .Returns(true);

            // Act
            Func<Task> act = () => _sut.ChangePasswordAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            user.PasswordHash.Should().Be("old-password-hash");

            _passwordHasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenDataIsValid_ShouldChangePassword()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                PasswordHash = "old-password-hash",
                IsActive = true
            };

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            const string newPasswordHash = "new-password-hash";

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Verify(dto.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _passwordHasherMock
                .Setup(h => h.Hash(dto.NewPassword))
                .Returns(newPasswordHash);

            var beforeChange = DateTime.UtcNow;

            // Act
            await _sut.ChangePasswordAsync(userId, dto);

            // Assert
            user.PasswordHash.Should().Be(newPasswordHash);
            user.UpdatedAt.Should().BeOnOrAfter(beforeChange);

            _passwordHasherMock.Verify(h => h.Verify(dto.CurrentPassword, "old-password-hash"), Times.Once);
            _passwordHasherMock.Verify(h => h.Hash(dto.NewPassword), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var dto = new ResetPasswordDto
            {
                NewPassword = "NewPassword123!"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.ResetPasswordAsync(userId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _passwordHasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenUserExists_ShouldResetPasswordAndUnlockUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                PasswordHash = "old-password-hash",
                FailedLoginAttempts = 5,
                LockoutUntil = DateTime.UtcNow.AddMinutes(30),
                IsActive = true
            };

            var dto = new ResetPasswordDto
            {
                NewPassword = "NewPassword123!"
            };

            const string newPasswordHash = "new-password-hash";

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(h => h.Hash(dto.NewPassword))
                .Returns(newPasswordHash);

            var beforeReset = DateTime.UtcNow;

            // Act
            await _sut.ResetPasswordAsync(userId, dto);

            // Assert
            user.PasswordHash.Should().Be(newPasswordHash);
            user.FailedLoginAttempts.Should().Be(0);
            user.LockoutUntil.Should().BeNull();
            user.UpdatedAt.Should().BeOnOrAfter(beforeReset);

            _passwordHasherMock.Verify(h => h.Hash(dto.NewPassword), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UnlockUserAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _sut.UnlockUserAsync(userId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UnlockUserAsync_WhenUserExists_ShouldUnlockUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                FailedLoginAttempts = 5,
                LockoutUntil = DateTime.UtcNow.AddMinutes(30),
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var beforeUnlock = DateTime.UtcNow;

            // Act
            await _sut.UnlockUserAsync(userId);

            // Assert
            user.FailedLoginAttempts.Should().Be(0);
            user.LockoutUntil.Should().BeNull();
            user.UpdatedAt.Should().BeOnOrAfter(beforeUnlock);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenUserExists_ShouldReturnUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Username = "jaouad",
                Email = "jaouad@test.com",
                SystemRole = SystemRole.Developer,
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _sut.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.Username.Should().Be("jaouad");
            result.Email.Should().Be("jaouad@test.com");
            result.SystemRole.Should().Be(SystemRole.Developer);
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _sut.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_WhenUserExists_ShouldReturnUserDto()
        {
            // Arrange
            const string email = "jaouad@test.com";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "jaouad",
                Email = email,
                SystemRole = SystemRole.Developer,
                IsActive = true
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _sut.GetByEmailAsync(email);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(email);
            result.Username.Should().Be("jaouad");
            result.SystemRole.Should().Be(SystemRole.Developer);
        }

        [Fact]
        public async Task GetByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            const string email = "unknown@test.com";

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _sut.GetByEmailAsync(email);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveUsersAsync_ShouldReturnActiveUsers()
        {
            // Arrange
            var users = new List<User>
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "developer1",
                        Email = "developer1@test.com",
                        SystemRole = SystemRole.Developer,
                        IsActive = true
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "developer2",
                        Email = "developer2@test.com",
                        SystemRole = SystemRole.Developer,
                        IsActive = true
                    }
                };

            _userRepositoryMock
                .Setup(r => r.GetActiveUsersAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _sut.GetActiveUsersAsync();

            // Assert
            result.Should().HaveCount(2);

            result.Should().Contain(u => u.Username == "developer1");
            result.Should().Contain(u => u.Username == "developer2");

            result.Should().OnlyContain(u => u.IsActive);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldReturnPagedUsers()
        {
            // Arrange
            var filter = new UserFilterDto
            {
                PageNumber = 2,
                PageSize = 10
            };

            var users = new List<User>
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "user1",
                        Email = "user1@test.com",
                        SystemRole = SystemRole.Developer,
                        IsActive = true
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "user2",
                        Email = "user2@test.com",
                        SystemRole = SystemRole.Admin,
                        IsActive = true
                    }
                };

            const int totalCount = 25;

            _userRepositoryMock
                .Setup(r => r.GetPaginatedAsync(filter))
                .ReturnsAsync((users, totalCount));

            // Act
            var result = await _sut.GetAllPaginatedAsync(filter);

            // Assert
            result.Items.Should().HaveCount(2);

            result.Items.Should().Contain(u => u.Username == "user1");
            result.Items.Should().Contain(u => u.Username == "user2");

            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);

            _userRepositoryMock.Verify(r => r.GetPaginatedAsync(filter), Times.Once);
        }


    }
}