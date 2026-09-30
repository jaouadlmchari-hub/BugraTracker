using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Common;

namespace BugTracker.Application.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Enregistre une nouvelle entrée d'audit via UnitOfWork.
    /// </summary>
    Task LogAsync(
        CreateAuditLogDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère les logs d'audit filtrés et paginés.
    /// </summary>
    Task<PagedResultDto<AuditLogDto>> GetLogsAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}