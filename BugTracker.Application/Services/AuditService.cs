using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Common;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Mappings; 
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(CreateAuditLogDto dto, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            UserEmail = dto.UserEmail,
            Action = dto.Action,
            EntityName = dto.EntityName,
            EntityId = dto.EntityId,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            Details = dto.Details,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<PagedResultDto<AuditLogDto>> GetLogsAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync(filter, cancellationToken);

        // Utilisation de notre méthode d'extension de mapping manuel
        var dtos = items.ToDtoList();

        return new PagedResultDto<AuditLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var auditLog = await _unitOfWork.AuditLogs.GetByIdAsync(id, cancellationToken);

        return auditLog?.ToDto();
    }
}