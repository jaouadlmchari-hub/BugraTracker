using BugTracker.Application.DTOs.Issues;
using BugTracker.Domain.Entities;

namespace BugTracker.Application.Mappings;

public static class IssueMappingExtensions
{
    public static IssueDto ToDto(this Issue issue)
    {
        return new IssueDto
        {
            Id = issue.Id,

            ProjectId = issue.ProjectId,
            EpicId = issue.EpicId,
            SprintId = issue.SprintId,

            ReporterId = issue.ReporterId,
            ReporterName = issue.Reporter.Username,

            AssigneeId = issue.AssigneeId,
            AssigneeName = issue.Assignee != null? issue.Assignee.Username: null,

            Title = issue.Title,
            Description = issue.Description,

            Type = issue.Type,
            Status = issue.Status,
            Priority = issue.Priority,

            StoryPoints = issue.StoryPoints,
            DueDate = issue.DueDate,
            DisplayOrder = issue.DisplayOrder,

            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt
        };
    }
}