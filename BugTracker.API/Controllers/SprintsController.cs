using BugTracker.Application.DTOs.Sprints;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/sprints")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly IAuthorizationService _authorizationService;

    public SprintsController(
        ISprintService sprintService, IAuthorizationService authorizationService)
    {
        _sprintService = sprintService;
        _authorizationService = authorizationService;
    }

    [HttpGet("{sprintId:guid}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> GetById(Guid sprintId)
    {
        var sprint = await _sprintService.GetByIdAsync(sprintId);

        if (sprint == null)
            return NotFound();


        var authorizationResult =
           await _authorizationService.AuthorizeAsync(
                 User,
                 sprint.ProjectId,
                 "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(sprint);
    }




    [HttpGet("/api/projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(IEnumerable<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SprintDto>>> GetAllByProject(Guid projectId)
    {

        var authorizationResult =
          await _authorizationService.AuthorizeAsync(
               User,
               projectId,
               "CanViewProject");
              
        if (!authorizationResult.Succeeded)
            return Forbid();

        var sprints = await _sprintService.GetAllByProjectAsync(projectId);

        return Ok(sprints);
    }



    [HttpPost("/api/projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SprintDto>> Create(Guid projectId, CreateSprintDto dto)
    {

        var authorizationResult =
           await _authorizationService.AuthorizeAsync(
               User,
               projectId,
               "CanManageProject");
          
        if (!authorizationResult.Succeeded)
            return Forbid();


        var sprint = await _sprintService.CreateAsync(projectId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { sprintId = sprint.Id },
            sprint);
    }




    [HttpPut("{sprintId:guid}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SprintDto>> Update(Guid sprintId, UpdateSprintDto dto)
    {

        var sprint = await _sprintService.GetByIdAsync(sprintId);

        if (sprint == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                  User,
                  sprint.ProjectId,
                  "CanManageProject");
            
        if (!authorizationResult.Succeeded)
            return Forbid();

        var updatedSprint =
               await _sprintService.UpdateAsync(sprintId, dto);

        return Ok(updatedSprint);
    }




    [HttpPatch("{sprintId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Start(Guid sprintId)
    {
        var sprint = await _sprintService.GetByIdAsync(sprintId);

        if (sprint == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                sprint.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();


        await _sprintService.StartAsync(sprintId);

        return NoContent();
    }




    [HttpPatch("{sprintId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(Guid sprintId)
    {
        var sprint = await _sprintService.GetByIdAsync(sprintId);

        if (sprint == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                sprint.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();


        await _sprintService.CompleteAsync(sprintId);

        return NoContent();
    }




    [HttpDelete("{sprintId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid sprintId)
    {
        var sprint = await _sprintService.GetByIdAsync(sprintId);

        if (sprint == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                sprint.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();


        await _sprintService.DeleteAsync(sprintId);

        return NoContent();
    }

}