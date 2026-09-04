namespace BugTracker.Application.Configuration
{
    public class AuthenticationSettings
    {
        public int MaxFailedLoginAttempts { get; set; }
        public int LockoutDurationMinutes { get; set; }
    }
}