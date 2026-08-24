using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/projects/{projectId:guid}/members")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProjectMembersController : ControllerBase
{
    private readonly IProjectMemberService _projectMemberService;
    private readonly IAuthorizationService _authorizationService;

    public ProjectMembersController(
        IProjectMemberService projectMemberService, IAuthorizationService authorizationService)
    {
        _projectMemberService = projectMemberService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectMemberDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProjectMemberDto>>> GetMembers(Guid projectId)
    {
        var authorizationResult =
           await _authorizationService.AuthorizeAsync(
                 User,
                 projectId,
                 "CanViewProject");
               
        if (!authorizationResult.Succeeded)
            return Forbid();


        var members =
            await _projectMemberService.GetMembersAsync(projectId);

        return Ok(members);
    }




    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectMemberDto>> GetMember(Guid projectId, Guid userId)
    {
        var authorizationResult =
         await _authorizationService.AuthorizeAsync(
               User,
               projectId,
               "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();



        var member = await _projectMemberService
            .GetMemberAsync(projectId, userId);

        if (member == null)
            return NotFound();

        return Ok(member);
    }




    [HttpPost]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(Guid projectId, AddProjectMemberDto dto)
    {
        var authorizationResult =
           await _authorizationService.AuthorizeAsync(
               User,
               projectId,
               "CanManageProject");
           
        if (!authorizationResult.Succeeded)
            return Forbid();



        var member = await _projectMemberService
            .AddMemberAsync(projectId, dto);

        return CreatedAtAction(
              nameof(GetMember),
              new
              {
                  projectId,
                  userId = member.UserId
              },
              member);
    }




    [HttpPatch("{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeRole(Guid projectId, Guid userId,ChangeProjectMemberRoleDto dto)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                projectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();


        await _projectMemberService.ChangeRoleAsync(
            projectId,
            userId,
            dto.NewRole);

        return NoContent();
    }




    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                projectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _projectMemberService.RemoveMemberAsync(
            projectId,
            userId);

        return NoContent();
    }
}