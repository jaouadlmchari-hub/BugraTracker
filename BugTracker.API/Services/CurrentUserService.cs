using System.Security.Claims;
using BugTracker.Application.Interfaces.Services;

namespace BugTracker.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?
                .User?
                .Identity?
                .IsAuthenticated == true;

        public Guid UserId
        {
            get
            {
                var userIdClaim =
                    _httpContextAccessor.HttpContext?
                        .User?
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }

                return Guid.Empty;
            }
        }

        public bool IsAdmin =>
            _httpContextAccessor.HttpContext?
                .User?
                .IsInRole("Admin") == true;
    }
}