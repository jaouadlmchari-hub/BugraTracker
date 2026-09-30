using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Common; 
using BugTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/audit-logs")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Récupère la liste paginée et filtrée des journaux d'audit (Entity, User, Date, Recherche, etc.).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetLogs(
        [FromQuery] AuditLogFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _auditService.GetLogsAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Récupère les détails d'un enregistrement d'audit spécifique par son identifiant.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogDto>> GetById(Guid id,CancellationToken cancellationToken)
    {
        var log = await _auditService.GetByIdAsync(id, cancellationToken);

        if (log == null)
            return NotFound();

        return Ok(log);
    }
}