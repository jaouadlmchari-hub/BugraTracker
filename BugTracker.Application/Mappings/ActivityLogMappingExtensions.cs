using BugTracker.Application.DTOs.ActivityLogs;
using BugTracker.Domain.Entities;


namespace BugTracker.Application.Mappings
{
    public static class ActivityLogMappingExtensions
    {
        public static ActivityLogDto ToDto(this ActivityLog log)
        {
            return new ActivityLogDto
            {
                Id = log.Id,
                IssueId = log.IssueId,
                UserId = log.UserId,
                UserName = log.User?.Username,
                Action = log.Action,
                Field = log.Field,
                FromValue = log.FromValue,
                ToValue = log.ToValue,
                CreatedAt = log.CreatedAt
            };
        }
    }
}
