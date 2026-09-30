using BugTracker.Application.DTOs.Audit;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings;

public static class AuditLogMappingExtensions
{
    /// <summary>
    /// Mappe une entité AuditLog vers un DTO AuditLogDto.
    /// </summary>
    public static AuditLogDto ToDto(this AuditLog entity)
    {
        return new AuditLogDto(
            entity.Id,
            entity.UserId,
            entity.UserEmail,
            entity.Action.ToString(), 
            entity.EntityName,
            entity.EntityId,
            entity.OldValue,
            entity.NewValue,
            entity.Details,
            entity.IpAddress,
            entity.UserAgent,
            entity.CreatedAtUtc
        );
    }

    /// <summary>
    /// Mappe une liste d'entités AuditLog vers une liste de DTOs AuditLogDto.
    /// </summary>
    public static IReadOnlyList<AuditLogDto> ToDtoList(this IEnumerable<AuditLog> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}