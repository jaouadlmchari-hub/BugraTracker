
namespace BugTracker.Application.DTOs.Comments
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid IssueId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
