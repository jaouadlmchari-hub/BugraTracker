using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace BugTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IAuthorizationService _authorizationService;

    public ProjectsController(
        IProjectService projectService, IAuthorizationService authorizationService)
    {
        _projectService = projectService;
        _authorizationService = authorizationService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var authorizationResult =
              await _authorizationService.AuthorizeAsync(
                     User,
                     id,
                    "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var project = await _projectService.GetByIdAsync(id);

        if (project == null)
            return NotFound();

        return Ok(project);
    }




    [HttpGet("key/{key}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetByKey(string key)
    {
        var project = await _projectService.GetByKeyAsync(key);

        if (project == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                 User,
                 project.Id,
                 "CanViewProject");
            
        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(project);
    }




    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<ProjectDto>>> GetAll(
    [FromQuery] ProjectFilterDto filter)
    {
        var result = await _projectService.GetAllPaginatedAsync(filter);

        return Ok(result);
    }



    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto dto)
    {
        var project = await _projectService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = project.Id },
            project);
    }



    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectDto dto)
    {

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                  User,
                  id,
                  "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var project = await _projectService.UpdateAsync(id, dto);

        return Ok(project);
    }



    [HttpPatch("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive(Guid id)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                id,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _projectService.ArchiveAsync(id);

        return NoContent();
    }




    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                id,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _projectService.ActivateAsync(id);

        return NoContent();
    }



    [HttpPatch("{id:guid}/owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeOwner(Guid id, ChangeProjectOwnerDto dto)
    {

        var project = await _projectService.GetByIdAsync(id);

        if (project == null)
            return NotFound();

        var authorizationResult =
        await _authorizationService.AuthorizeAsync(
            User,
            project,
            "CanChangeProjectOwner");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _projectService.ChangeOwnerAsync(
            id,
            dto.NewOwnerId);

        return NoContent();
    }




    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _projectService.DeleteAsync(id);

        return NoContent();
    }
}