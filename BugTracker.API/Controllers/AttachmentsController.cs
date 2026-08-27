using BugTracker.Application.DTOs.Attachments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/attachments")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    private readonly IIssueService _issueService;
    private readonly IAuthorizationService _authorizationService;

    public AttachmentsController(
     IAttachmentService attachmentService,
     IIssueService issueService,
     IAuthorizationService authorizationService)
    {
        _attachmentService = attachmentService;
        _issueService = issueService;
        _authorizationService = authorizationService;
    }


    [HttpGet("{attachmentId:guid}")]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentDto>> GetById(Guid attachmentId)
    {
        var attachment =
            await _attachmentService.GetByIdAsync(attachmentId);

        if (attachment == null)
            return NotFound();

        var issue =
            await _issueService.GetByIdAsync(attachment.IssueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        return Ok(attachment);
    }




    [HttpGet("/api/issues/{issueId:guid}/attachments")]
    [ProducesResponseType(typeof(IEnumerable<AttachmentDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<AttachmentDto>>> GetByIssue(Guid issueId)
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

        var attachments =
            await _attachmentService.GetByIssueAsync(issueId);

        return Ok(attachments);
    }




    [HttpPost("/api/issues/{issueId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid issueId,IFormFile file)
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

        if (file == null || file.Length == 0)
            return BadRequest("Aucun fichier fourni.");

        await using var stream = file.OpenReadStream();

        var dto = new CreateAttachmentDto
        {
            FileContent = stream,
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var attachment =
            await _attachmentService.UploadAsync(issueId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { attachmentId = attachment.Id },
            attachment);
    }




    [HttpGet("{attachmentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Download(Guid attachmentId)
    {
        var attachment =
     await _attachmentService.GetByIdAsync(attachmentId);

        if (attachment == null)
            return NotFound();

        var issue =
            await _issueService.GetByIdAsync(attachment.IssueId);

        if (issue == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                issue.ProjectId,
                "CanViewProject");

        if (!authorizationResult.Succeeded)
            return Forbid();

        var downloadUrl =
            await _attachmentService.GetDownloadUrlAsync(attachmentId);

        return Ok(new
        {
            DownloadUrl = downloadUrl
        });
    }




    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid attachmentId)
    {
        var attachment =
            await _attachmentService.GetByIdAsync(attachmentId);

        if (attachment == null)
            return NotFound();

        var authorizationResult =
            await _authorizationService.AuthorizeAsync(
                User,
                attachment,
                "CanDeleteAttachment");

        if (!authorizationResult.Succeeded)
            return Forbid();

        await _attachmentService.DeleteAsync(attachmentId);

        return NoContent();
    }
}