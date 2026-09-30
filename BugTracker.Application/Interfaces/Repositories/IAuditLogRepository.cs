using BugTracker.Application.DTOs.Audit;
using BugTracker.Domain.Entities;

namespace BugTracker.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default);
}