

using BugTracker.Application.DTOs.Comments;

namespace BugTracker.Application.Interfaces.Services
{
    public interface ICommentService 
    {
        Task<CommentDto> CreateAsync(Guid issueId, CreateCommentDto dto);

        Task<CommentDto> UpdateAsync(Guid commentId, UpdateCommentDto dto);

        Task DeleteAsync(Guid commentId);
    }
}
