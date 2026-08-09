using BugTracker.Application.DTOs.Users;
using BugTracker.Application.DTOs.Common;
using BugTracker.Domain.Enums;

public interface IUserService
{
    // Consultation
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetActiveUsersAsync();
    Task<PagedResultDto<UserDto>> GetAllPaginatedAsync(UserFilterDto filter);

    // Création
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> AdminCreateAsync(AdminCreateUserDto dto);

    // Modification du profil
    Task<UserDto> UpdateAsync(Guid userId, UpdateUserDto dto);

    // Administration
    Task DeactivateAsync(Guid userId);
    Task ActivateAsync(Guid userId);
    Task ChangeSystemRoleAsync(Guid userId, SystemRole newRole);

    // Sécurité
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task UnlockUserAsync(Guid userId);
}