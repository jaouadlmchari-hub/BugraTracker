namespace BugTracker.Application.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,           
    string EntityName,
    string? EntityId,
    string? OldValue,      
    string? NewValue,       
    string? Details,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAtUtc);