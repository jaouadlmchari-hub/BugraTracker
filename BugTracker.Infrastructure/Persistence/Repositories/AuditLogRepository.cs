using BugTracker.Application.DTOs.Audit;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Interfaces.Repositories;
using BugTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly BugTrackerDbContext _context;

    public AuditLogRepository(BugTrackerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.Set<AuditLog>().AddAsync(auditLog, cancellationToken);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<AuditLog>().AsNoTracking().AsQueryable();

        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.UserEmail))
            query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(filter.UserEmail));

        if (filter.Action.HasValue)
            query = query.Where(a => a.Action == filter.Action.Value);

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
            query = query.Where(a => a.EntityName == filter.EntityName);

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.CreatedAtUtc >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(a => a.CreatedAtUtc <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                (a.Details != null && a.Details.ToLower().Contains(term)) ||
                (a.IpAddress != null && a.IpAddress.ToLower().Contains(term)) ||
                (a.UserAgent != null && a.UserAgent.ToLower().Contains(term)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}