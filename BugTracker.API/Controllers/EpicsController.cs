using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/epics")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class EpicsController : ControllerBase
{
    private readonly IEpicService _epicService;
    private readonly IAuthorizationService _authorizationService;


    public EpicsController(IEpicService epicService, IAuthorizationService authorizationService)
    {
        _epicService = epicService;
        _authorizationService = authorizationService;
    }




    [HttpGet("{epicId:guid}")]
    [ProducesResponseType(typeof(EpicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EpicDto>> GetById(Guid epicId)
    {
        var epic = await _epicService.GetByIdAsync(epicId);

        if (epic == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                epic.ProjectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(epic);
    }




    [HttpGet("{epicId:guid}/details")]
    [ProducesResponseType(typeof(EpicDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EpicDetailsDto>> GetByIdWithDetails(Guid epicId)
    {
        var epic = await _epicService.GetByIdWithDetailsAsync(epicId);

        if (epic == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                epic.ProjectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(epic);
    }





    [HttpGet("/api/projects/{projectId:guid}/epics")]
    [ProducesResponseType(typeof(IEnumerable<EpicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EpicDto>>> GetAllByProject(Guid projectId)
    {
        var authorizationResult =
           await _authorizationService.AuthorizeAsync(
               User,
               projectId,
               "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var epics = await _epicService.GetAllByProjectAsync(projectId);

        return Ok(epics);
    }



    [HttpGet("/api/projects/{projectId:guid}/epics/active")]
    [ProducesResponseType(typeof(IEnumerable<EpicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EpicDto>>> GetActiveByProject(Guid projectId)
    {

        var authorizationResult =
           await _authorizationService.AuthorizeAsync(
               User,
               projectId,
               "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var epics = await _epicService.GetActiveByProjectAsync(projectId);

        return Ok(epics);
    }




    [HttpPost("/api/projects/{projectId:guid}/epics")]
    [ProducesResponseType(typeof(EpicDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EpicDto>> Create(Guid projectId, CreateEpicDto dto)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                projectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();


        var epic = await _epicService.CreateAsync(
            projectId,
            dto);

        return CreatedAtAction(
            nameof(GetById),
            new { epicId = epic.Id },
            epic);
    }





    [HttpPut("{epicId:guid}")]
    [ProducesResponseType(typeof(EpicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EpicDto>> Update(Guid epicId, UpdateEpicDto dto)
    {

        var epic = await _epicService.GetByIdAsync(epicId);

        if (epic == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                epic.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var updatedEpic =
            await _epicService.UpdateAsync(epicId, dto);

        return Ok(updatedEpic);
    }





    [HttpPatch("{epicId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(Guid epicId, ChangeEpicStatusDto dto)
    {

        var epic = await _epicService.GetByIdAsync(epicId);

        if (epic == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                epic.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();


        await _epicService.ChangeStatusAsync(
            epicId,
            dto.NewStatus);

        return NoContent();
    }





    [HttpDelete("{epicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid epicId)
    {
        var epic = await _epicService.GetByIdAsync(epicId);

        if (epic == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                epic.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _epicService.DeleteAsync(epicId);

        return NoContent();
    }
}