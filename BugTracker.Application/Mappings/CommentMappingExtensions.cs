

using BugTracker.Application.DTOs.Comments;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings
{
    public static  class CommentMappingExtensions
    {

        public static CommentDto ToDto(this Comment dto)
        {
            return new CommentDto
            {
                Id = dto.Id,
                IssueId = dto.IssueId,
                AuthorId = dto.AuthorId,
                Content = dto.Content,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }

    }
}
