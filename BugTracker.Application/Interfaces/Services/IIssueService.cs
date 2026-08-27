using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Domain.Enums;

public interface IIssueService
{
    Task<IssueDto?> GetByIdAsync(Guid issueId);

    Task<PagedResultDto<IssueDto>> GetByProjectPaginatedAsync(Guid projectId, IssueFilterDto filter);

    Task<IssueDto> CreateAsync(Guid projectId, CreateIssueDto dto);

    Task<IssueDto> UpdateAsync(Guid issueId, UpdateIssueDto dto);

    Task ChangeStatusAsync(Guid issueId, IssueStatus newStatus);

    Task ChangeStoryPointsAsync(Guid issueId, int? storyPoints);

    Task AssignAsync(Guid issueId, Guid userId);

    Task MoveToSprintAsync(Guid issueId, Guid? sprintId);

    Task MoveToEpicAsync(Guid issueId, Guid? epicId);

    Task ReorderAsync(IEnumerable<ReorderIssueItemDto> items);

    Task DeleteAsync(Guid issueId);
}