using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Users;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found",
                    id);

                return null;
            }

            return user.ToDto();
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning(
                    "User not found for email {Email}",
                    email);

                return null;
            }

            return user.ToDto();
        }

        public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
        {
            var users = await _unitOfWork.Users.GetActiveUsersAsync();

            _logger.LogInformation(
                "Retrieved {UserCount} active users",
                users.Count());

            return users.Select(user => user.ToDto());
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            _logger.LogInformation(
                "Creating new user with username {Username}",
                dto.Username);

            var isEmailUnique =
                await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);

            if (!isEmailUnique)
            {
                _logger.LogWarning(
                    "User creation failed: email {Email} already exists",
                    dto.Email);

                throw new ConflictException(
                    "Email is already in use.");
            }

            var isUsernameUnique =
                await _unitOfWork.Users.IsUsernameUniqueAsync(dto.Username);

            if (!isUsernameUnique)
            {
                _logger.LogWarning(
                    "User creation failed: username {Username} already exists",
                    dto.Username);

                throw new ConflictException(
                    "Username is already in use.");
            }

            var hashedPassword =
                _passwordHasher.Hash(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                Username = dto.Username,
                FullName = dto.FullName,
                PasswordHash = hashedPassword,
                AvatarUrl = dto.AvatarUrl,
                SystemRole = SystemRole.Developer
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} created successfully with username {Username}",
                user.Id,
                user.Username);

            return user.ToDto();
        }

        public async Task<UserDto> UpdateAsync(
            Guid userId,
            UpdateUserDto dto)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Update failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "User not found.");
            }

            var isEmailUnique =
                await _unitOfWork.Users
                    .IsEmailUniqueAsync(dto.Email, userId);

            if (!isEmailUnique)
            {
                _logger.LogWarning(
                    "Update failed for user {UserId}: email {Email} already exists",
                    userId,
                    dto.Email);

                throw new ConflictException(
                    "Email is already in use.");
            }

            var isUsernameUnique =
                await _unitOfWork.Users
                    .IsUsernameUniqueAsync(dto.Username, userId);

            if (!isUsernameUnique)
            {
                _logger.LogWarning(
                    "Update failed for user {UserId}: username {Username} already exists",
                    userId,
                    dto.Username);

                throw new ConflictException(
                    "Username is already in use.");
            }

            user.Email = dto.Email;
            user.Username = dto.Username;
            user.FullName = dto.FullName;
            user.AvatarUrl = dto.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} updated successfully",
                userId);

            return user.ToDto();
        }

        public async Task DeactivateAsync(Guid userId)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Deactivation failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "User not found.");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} deactivated successfully",
                userId);
        }

        public async Task ActivateAsync(Guid userId)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Activation failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "User not found.");
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} activated successfully",
                userId);
        }

        public async Task<UserDto> AdminCreateAsync(AdminCreateUserDto dto)
        {
            _logger.LogInformation(
                "Admin creating user with username {Username} and role {SystemRole}",
                dto.Username,
                dto.SystemRole);

            var isEmailUnique =
                await _unitOfWork.Users
                    .IsEmailUniqueAsync(dto.Email);

            if (!isEmailUnique)
            {
                _logger.LogWarning(
                    "Admin user creation failed: email {Email} already exists",
                    dto.Email);

                throw new ConflictException(
                    "Email is already in use.");
            }

            var isUsernameUnique =
                await _unitOfWork.Users
                    .IsUsernameUniqueAsync(dto.Username);

            if (!isUsernameUnique)
            {
                _logger.LogWarning(
                    "Admin user creation failed: username {Username} already exists",
                    dto.Username);

                throw new ConflictException(
                    "Username is already in use.");
            }

            var hashedPassword =
                _passwordHasher.Hash(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                Username = dto.Username,
                FullName = dto.FullName,
                PasswordHash = hashedPassword,
                AvatarUrl = dto.AvatarUrl,
                SystemRole = dto.SystemRole
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Admin successfully created user {UserId} with role {SystemRole}",
                user.Id,
                user.SystemRole);

            return user.ToDto();
        }

        public async Task<PagedResultDto<UserDto>> GetAllPaginatedAsync(UserFilterDto filter)
        {
            var (users, totalCount) =
                await _unitOfWork.Users
                    .GetPaginatedAsync(filter);

            var userDtos = users
                .Select(u => u.ToDto())
                .ToList();

            _logger.LogInformation(
                "Retrieved paginated users. Page {PageNumber}, PageSize {PageSize}, TotalCount {TotalCount}",
                filter.PageNumber,
                filter.PageSize,
                totalCount);

            return new PagedResultDto<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task ChangeSystemRoleAsync(Guid userId, SystemRole newRole)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Role change failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "User not found.");
            }

            if (!Enum.IsDefined(typeof(SystemRole), newRole))
            {
                _logger.LogWarning(
                    "Role change failed: invalid role {SystemRole} for user {UserId}",
                    newRole,
                    userId);

                throw new BusinessRuleException(
                    "Invalid system role.");
            }

            var oldRole = user.SystemRole;

            user.SystemRole = newRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "System role changed for user {UserId}: {OldRole} -> {NewRole}",
                userId,
                oldRole,
                newRole);
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Password change failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "Utilisateur non trouvé.");
            }

            var isCurrentPasswordValid =
                _passwordHasher.Verify(
                    dto.CurrentPassword,
                    user.PasswordHash);

            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning(
                    "Password change failed: invalid current password for user {UserId}",
                    userId);

                throw new BusinessRuleException(
                    "Le mot de passe actuel est incorrect.");
            }

            var isSamePassword =
                _passwordHasher.Verify(
                    dto.NewPassword,
                    user.PasswordHash);

            if (isSamePassword)
            {
                _logger.LogWarning(
                    "Password change failed: new password is identical to current password for user {UserId}",
                    userId);

                throw new BusinessRuleException(
                    "Le nouveau mot de passe doit être différent de l'ancien.");
            }

            user.PasswordHash =
                _passwordHasher.Hash(dto.NewPassword);

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Password changed successfully for user {UserId}",
                userId);
        }

        public async Task ResetPasswordAsync(Guid userId, ResetPasswordDto dto)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Password reset failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "Utilisateur introuvable.");
            }

            user.PasswordHash =
                _passwordHasher.Hash(dto.NewPassword);

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Password reset successfully for user {UserId}",
                userId);
        }

        public async Task UnlockUserAsync(Guid userId)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Unlock failed: user {UserId} not found",
                    userId);

                throw new NotFoundException(
                    "Utilisateur non trouvé.");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} unlocked successfully",
                userId);
        }
    }
}