using BugTracker.Domain.Enums;
using BugTracker.Application.DTOs.Issues;

public interface IIssueService
{
    Task<IssueDto?> GetByIdAsync(Guid issueId);

    Task<IEnumerable<IssueDto>> GetByProjectAsync(Guid projectId);

    Task<IssueDto> CreateAsync(Guid projectId, CreateIssueDto dto);

    Task<IssueDto> UpdateAsync(Guid issueId, UpdateIssueDto dto);

    Task ChangeStatusAsync(Guid issueId, IssueStatus newStatus);

    Task AssignAsync(Guid issueId, Guid userId);

    Task MoveToSprintAsync(Guid issueId, Guid? sprintId);

    Task DeleteAsync(Guid issueId);
}