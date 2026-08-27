using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/issues")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;
    private readonly IAuthorizationService _authorizationService;

    public IssuesController(IIssueService issueService, IAuthorizationService authorizationService)
    {
        _issueService = issueService;
        _authorizationService = authorizationService;
    }


    [HttpGet("{issueId:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> GetById(Guid issueId)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(issue);
    }




    [HttpGet("/api/projects/{projectId:guid}/issues")]
    [ProducesResponseType(typeof(PagedResultDto<IssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<IssueDto>>> GetByProject(
        Guid projectId,
        [FromQuery] IssueFilterDto filter)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                projectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var result =
            await _issueService.GetByProjectPaginatedAsync(
                projectId,
                filter);

        return Ok(result);
    }





    [HttpPost("/api/projects/{projectId:guid}/issues")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IssueDto>> Create(Guid projectId, CreateIssueDto dto)
    {
        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                projectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var issue =
            await _issueService.CreateAsync(projectId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { issueId = issue.Id },
            issue);
    }





    [HttpPut("{issueId:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IssueDto>> Update(Guid issueId, UpdateIssueDto dto)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue,
                "CanEditIssue");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var updatedIssue =
            await _issueService.UpdateAsync(issueId, dto);

        return Ok(updatedIssue);
    }





    [HttpPatch("{issueId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(Guid issueId, ChangeIssueStatusDto dto)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue,
                "CanChangeIssueStatus");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.ChangeStatusAsync(
            issueId,
            dto.NewStatus);

        return NoContent();
    }





    [HttpPatch("{issueId:guid}/story-points")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStoryPoints(Guid issueId, ChangeStoryPointsDto dto)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue,
                "CanEditIssue");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.ChangeStoryPointsAsync(
            issueId,
            dto.StoryPoints);

        return NoContent();
    }





    [HttpPatch("{issueId:guid}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(Guid issueId, AssignIssueDto dto)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.AssignAsync(
            issueId,
            dto.UserId);

        return NoContent();
    }





    [HttpPatch("{issueId:guid}/sprint")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MoveToSprint(Guid issueId, MoveIssueToSprintDto dto)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.MoveToSprintAsync(
            issueId,
            dto.SprintId);

        return NoContent();
    }





    [HttpPatch("{issueId:guid}/epic")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MoveToEpic(Guid issueId, MoveIssueToEpicDto dto)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.MoveToEpicAsync(
            issueId,
            dto.EpicId);

        return NoContent();
    }




    [HttpPatch("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reorder(ReorderIssuesDto dto)
    {
        var firstItem = dto.Items.First();

        var issue = await _issueService.GetByIdAsync(firstItem.IssueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.ReorderAsync(dto.Items);

        return NoContent();
    }





    [HttpDelete("{issueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid issueId)
    {
        var issue = await _issueService.GetByIdAsync(issueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanManageProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _issueService.DeleteAsync(issueId);

        return NoContent();
    }






}