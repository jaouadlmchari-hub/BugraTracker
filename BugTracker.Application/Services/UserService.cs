using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Users;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;


namespace BugTracker.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;

            _passwordHasher = passwordHasher;

        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);

            if (user == null)
                return null;

            return user.ToDto();
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
           var user = await  _unitOfWork.Users.GetByEmailAsync(email);

            if (user == null)
                return null;

            return user.ToDto();
        }

        public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
        {
            var users = await _unitOfWork.Users.GetActiveUsersAsync();

            return users.Select(user => user.ToDto());
        }
        
        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);

            if (!isUnique)
                throw new InvalidOperationException("Email is already in use.");

            var hashedPassword = _passwordHasher.Hash(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                Username = dto.Username,
                FullName = dto.FullName,
                PasswordHash = hashedPassword,
                AvatarUrl = dto.AvatarUrl
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return user.ToDto();
        }

        public async Task <UserDto> UpdateAsync(Guid userId , UpdateUserDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email, userId);

            if (!isUnique)
                throw new InvalidOperationException("Email is already in use.");

            user.Email = dto.Email;
            user.Username = dto.Username;
            user.FullName = dto.FullName;
            user.AvatarUrl = dto.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return user.ToDto();

        }

        public async Task DeactivateAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ActivateAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<UserDto> AdminCreateAsync(AdminCreateUserDto dto)
        {
            var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);

            if (!isUnique)
                throw new InvalidOperationException("Email is already in use.");

            var hashedPassword = _passwordHasher.Hash(dto.Password);

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

            return user.ToDto();
        }

        public async Task<PagedResultDto<UserDto>> GetAllPaginatedAsync(UserFilterDto filter)
        {
            var (users, totalCount) = await _unitOfWork.Users.GetPaginatedAsync(filter);

            var userDtos = users
                .Select(u => u.ToDto())
                .ToList();

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
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null )
                throw new KeyNotFoundException("User not found.");

            if (!Enum.IsDefined(typeof(SystemRole), newRole))
                throw new ArgumentException("Invalid system role.");

            user.SystemRole = newRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
           
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Utilisateur non trouvé.");

            
            var isCurrentPasswordValid = _passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
                throw new UnauthorizedAccessException("Le mot de passe actuel est incorrect.");

            var isSamePassword = _passwordHasher.Verify(dto.NewPassword, user.PasswordHash);
            if (isSamePassword)
                throw new InvalidOperationException("Le nouveau mot de passe doit être différent de l'ancien.");

            user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            user.UpdatedAt = DateTime.UtcNow;

           
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UnlockUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                throw new KeyNotFoundException("Utilisateur non trouvé.");

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
