using BugTracker.Domain.Enums;

namespace BugTracker.Application.DTOs.Audit;

public record AuditLogFilterDto
{
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public AuditAction? Action { get; init; }
    public string? EntityName { get; init; }
    public string? EntityId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? SearchTerm { get; init; }

    // Pagination (avec valeurs par défaut)
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}