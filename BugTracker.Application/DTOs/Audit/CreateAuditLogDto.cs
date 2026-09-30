using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Audit;

public record CreateAuditLogDto(
    Guid? UserId,
    string? UserEmail,
    AuditAction Action,         
    string EntityName,
    string? EntityId = null,
    string? OldValue = null,
    string? NewValue = null,
    string? Details = null,
    string? IpAddress = null,
    string? UserAgent = null);