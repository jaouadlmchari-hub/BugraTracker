using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/comments")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IIssueService _issueService;
        private readonly IAuthorizationService _authorizationService;

        public CommentsController(
             ICommentService commentService,
             IIssueService issueService,
             IAuthorizationService authorizationService)
        {
            _commentService = commentService;
            _issueService = issueService;
            _authorizationService = authorizationService;
        }


        [HttpGet("{commentId:guid}")]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CommentDto>> GetById(Guid commentId)
        {
            var comment = await _commentService.GetByIdAsync(commentId);

            if (comment == null)
                return NotFound();

            var issue = await _issueService.GetByIdAsync(comment.IssueId);

            if (issue == null)
                return NotFound();

            var authorizationResult =
                await _authorizationService.AuthorizeAsync(
                    User,
                    issue.ProjectId,
                    "CanViewProject");

            if (!authorizationResult.Succeeded)
                return Forbid();

            return Ok(comment);
        }




        [HttpGet("/api/issues/{issueId:guid}/comments")]
        [ProducesResponseType(typeof(IEnumerable<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetByIssue(Guid issueId)
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

            var comments =
                await _commentService.GetByIssueAsync(issueId);

            return Ok(comments);
        }





        [HttpPost("/api/issues/{issueId:guid}/comments")]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<CommentDto>> Create(Guid issueId, CreateCommentDto dto)
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

            var comment = await _commentService.CreateAsync(
                issueId,
                dto);

            return CreatedAtAction(
                nameof(GetById),
                new { commentId = comment.Id },
                comment);
        }




        [HttpPut("{commentId:guid}")]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<CommentDto>> Update(Guid commentId, UpdateCommentDto dto)
        {
            var comment = await _commentService.GetByIdAsync(commentId);

            if (comment == null)
                return NotFound();

            var authorizationResult =
                await _authorizationService.AuthorizeAsync(
                    User,
                    comment,
                    "CanEditComment");

            if (!authorizationResult.Succeeded)
                return Forbid();

            var updatedComment =
                await _commentService.UpdateAsync(commentId, dto);

            return Ok(updatedComment);
        }




        [HttpDelete("{commentId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid commentId)
        {
            var comment = await _commentService.GetByIdAsync(commentId);

            if (comment == null)
                return NotFound();

            var authorizationResult =
                await _authorizationService.AuthorizeAsync(
                    User,
                    comment,
                    "CanDeleteComment");

            if (!authorizationResult.Succeeded)
                return Forbid();

            await _commentService.DeleteAsync(commentId);

            return NoContent();
        }
    }
}
