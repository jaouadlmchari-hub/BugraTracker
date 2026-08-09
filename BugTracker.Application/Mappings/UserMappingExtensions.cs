using BugTracker.Application.DTOs.Users;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings
{
    public static class UserMappingExtensions
    {
        public static UserDto ToDto(this User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                SystemRole = user.SystemRole,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
