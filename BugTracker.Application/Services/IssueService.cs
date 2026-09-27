using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services
{
    public class IssueService : IIssueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<IssueService> _logger;

        public IssueService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IActivityLogService activityLogService,
            ILogger<IssueService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        public async Task<IssueDto?> GetByIdAsync(Guid issueId)
        {
            _logger.LogDebug(
                "Getting issue by Id. IssueId: {IssueId}",
                issueId);

            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Issue not found. IssueId: {IssueId}",
                    issueId);

                return null;
            }

            return issue.ToDto();
        }

        public async Task<PagedResultDto<IssueDto>> GetByProjectPaginatedAsync(
            Guid projectId,
            IssueFilterDto filter)
        {
            _logger.LogDebug(
                "Getting paginated issues. ProjectId: {ProjectId}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                projectId,
                filter.PageNumber,
                filter.PageSize);

            var projectExists =
                await _unitOfWork.Projects.ExistsAsync(projectId);

            if (!projectExists)
            {
                _logger.LogWarning(
                    "Project not found while retrieving issues. ProjectId: {ProjectId}",
                    projectId);

                throw new NotFoundException("Projet introuvable.");
            }

            var (issues, totalCount) =
                await _unitOfWork.Issues.GetPaginatedAsync(
                    projectId,
                    filter);

            return new PagedResultDto<IssueDto>
            {
                Items = issues
                    .Select(i => i.ToDto())
                    .ToList(),

                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        private async Task ValidateEpicAsync(
            Guid projectId,
            Guid epicId)
        {
            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
            {
                _logger.LogWarning(
                    "Epic not found. EpicId: {EpicId}, ProjectId: {ProjectId}",
                    epicId,
                    projectId);

                throw new NotFoundException("Epic non trouvé.");
            }

            if (epic.ProjectId != projectId)
            {
                _logger.LogWarning(
                    "Epic does not belong to project. EpicId: {EpicId}, ProjectId: {ProjectId}",
                    epicId,
                    projectId);

                throw new BusinessRuleException(
                    "L'Epic n'appartient pas au même projet que l'Issue.");
            }

            if (epic.Status == EpicStatus.Archived)
            {
                _logger.LogWarning(
                    "Attempt to assign issue to archived Epic. EpicId: {EpicId}",
                    epicId);

                throw new BusinessRuleException(
                    "Impossible d'affecter une Issue à un Epic archivé.");
            }
        }

        public async Task<IssueDto> CreateAsync(
            Guid projectId,
            CreateIssueDto dto)
        {
            var currentUserId = _currentUserService.UserId;

            _logger.LogInformation(
                "Creating issue. ProjectId: {ProjectId}, UserId: {UserId}, Title: {Title}",
                projectId,
                currentUserId,
                dto.Title);

            await using var transaction =
                await _unitOfWork.BeginTransactionAsync();

            try
            {
                var project = await _unitOfWork.Projects
                    .GetByIdAsync(projectId);

                if (project == null)
                {
                    _logger.LogWarning(
                        "Cannot create issue because project was not found. ProjectId: {ProjectId}",
                        projectId);

                    throw new NotFoundException("Projet non trouvé.");
                }

                if (dto.SprintId.HasValue)
                {
                    var sprint = await _unitOfWork.Sprints
                        .GetByIdAsync(dto.SprintId.Value);

                    if (sprint == null)
                    {
                        _logger.LogWarning(
                            "Sprint not found while creating issue. SprintId: {SprintId}",
                            dto.SprintId.Value);

                        throw new NotFoundException("Sprint non trouvé.");
                    }

                    if (sprint.ProjectId != projectId)
                    {
                        _logger.LogWarning(
                            "Sprint does not belong to project. SprintId: {SprintId}, ProjectId: {ProjectId}",
                            dto.SprintId.Value,
                            projectId);

                        throw new BusinessRuleException(
                            "Le sprint n'appartient pas à ce projet.");
                    }
                }

                if (dto.EpicId.HasValue)
                {
                    await ValidateEpicAsync(
                        projectId,
                        dto.EpicId.Value);
                }

                if (dto.AssigneeId.HasValue)
                {
                    var assignee = await _unitOfWork.ProjectMembers
                        .GetByProjectAndUserAsync(
                            projectId,
                            dto.AssigneeId.Value);

                    if (assignee == null)
                    {
                        _logger.LogWarning(
                            "Assignee is not a member of project. ProjectId: {ProjectId}, AssigneeId: {AssigneeId}",
                            projectId,
                            dto.AssigneeId.Value);

                        throw new BusinessRuleException(
                            "L'utilisateur assigné doit être membre du projet.");
                    }
                }

                var issue = new Issue
                {
                    ProjectId = projectId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Type = dto.Type,
                    Priority = dto.Priority ?? Priority.Medium,
                    Status = IssueStatus.Todo,
                    StoryPoints = dto.StoryPoints,
                    DueDate = dto.DueDate,
                    EpicId = dto.EpicId,
                    SprintId = dto.SprintId,
                    ReporterId = currentUserId,
                    AssigneeId = dto.AssigneeId,
                    DisplayOrder = 0
                };

                await _unitOfWork.Issues.AddAsync(issue);

                // SQL Server génère l'Id avec NEWID()
                await _unitOfWork.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    issue.Id,
                    currentUserId,
                    ActivityAction.Created);

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Issue created successfully. IssueId: {IssueId}, ProjectId: {ProjectId}, UserId: {UserId}",
                    issue.Id,
                    projectId,
                    currentUserId);

                var createdIssue =
                    await _unitOfWork.Issues
                        .GetByIdWithDetailsAsync(issue.Id);

                if (createdIssue == null)
                {
                    _logger.LogError(
                        "Issue was created but could not be retrieved afterward. IssueId: {IssueId}",
                        issue.Id);

                    throw new NotFoundException(
                        "Impossible de récupérer l'issue créée.");
                }

                return createdIssue.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while creating issue. ProjectId: {ProjectId}, UserId: {UserId}, Title: {Title}",
                    projectId,
                    currentUserId,
                    dto.Title);

                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IssueDto> UpdateAsync(
            Guid issueId,
            UpdateIssueDto dto)
        {
            var currentUserId = _currentUserService.UserId;

            _logger.LogInformation(
                "Updating issue. IssueId: {IssueId}, UserId: {UserId}",
                issueId,
                currentUserId);

            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot update issue because it was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException("Issue non trouvée.");
            }

            issue.Title = dto.Title;
            issue.Description = dto.Description;
            issue.Type = dto.Type;
            issue.Priority = dto.Priority;
            issue.StoryPoints = dto.StoryPoints;
            issue.DueDate = dto.DueDate;
            issue.UpdatedAt = DateTime.UtcNow;

            if (dto.EpicId != issue.EpicId)
            {
                if (dto.EpicId.HasValue)
                {
                    await ValidateEpicAsync(
                        issue.ProjectId,
                        dto.EpicId.Value);
                }

                issue.EpicId = dto.EpicId;
            }

            await _unitOfWork.SaveChangesAsync();

            var updatedIssue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (updatedIssue == null)
            {
                _logger.LogError(
                    "Issue updated but could not be retrieved afterward. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Impossible de récupérer l'issue modifiée.");
            }

            _logger.LogInformation(
                "Issue updated successfully. IssueId: {IssueId}, UserId: {UserId}",
                issueId,
                currentUserId);

            return updatedIssue.ToDto();
        }

        public async Task ChangeStatusAsync(
            Guid issueId,
            IssueStatus newStatus)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot change status because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException("Issue non trouvée.");
            }

            if (!Enum.IsDefined(typeof(IssueStatus), newStatus))
            {
                _logger.LogWarning(
                    "Invalid issue status provided. IssueId: {IssueId}, Status: {Status}",
                    issueId,
                    newStatus);

                throw new BusinessRuleException(
                    "Le statut fourni est invalide.");
            }

            if (issue.SprintId.HasValue)
            {
                var sprint = await _unitOfWork.Sprints
                    .GetByIdAsync(issue.SprintId.Value);

                if (sprint != null &&
                    sprint.Status == SprintStatus.Completed)
                {
                    _logger.LogWarning(
                        "Attempt to change status of issue in completed sprint. IssueId: {IssueId}, SprintId: {SprintId}",
                        issueId,
                        issue.SprintId.Value);

                    throw new BusinessRuleException(
                        "Une issue appartenant à un sprint terminé ne peut plus changer de statut.");
                }
            }

            var currentUserId = _currentUserService.UserId;

            if (issue.Status == IssueStatus.Done &&
                newStatus == IssueStatus.Todo &&
                !_currentUserService.IsAdmin)
            {
                var currentMember =
                    await _unitOfWork.ProjectMembers
                        .GetByProjectAndUserAsync(
                            issue.ProjectId,
                            currentUserId);

                var canReopen =
                    currentMember != null &&
                    (
                        currentMember.Role == ProjectRole.Manager ||
                        (
                            currentMember.Role == ProjectRole.QA &&
                            issue.Type == IssueType.Bug
                        )
                    );

                if (!canReopen)
                {
                    _logger.LogWarning(
                        "Unauthorized issue reopening attempt. IssueId: {IssueId}, UserId: {UserId}",
                        issueId,
                        currentUserId);

                    throw new ForbiddenException(
                        "Seuls les QA, PM et Admin peuvent rouvrir cette issue.");
                }
            }

            if (!IsValidTransition(
                    issue.Status,
                    newStatus))
            {
                _logger.LogWarning(
                    "Invalid issue status transition. IssueId: {IssueId}, From: {OldStatus}, To: {NewStatus}",
                    issueId,
                    issue.Status,
                    newStatus);

                throw new BusinessRuleException(
                    "INVALID_STATUS_TRANSITION");
            }

            var oldStatus = issue.Status;

            issue.Status = newStatus;

            await _activityLogService.LogAsync(
                issue.Id,
                currentUserId,
                ActivityAction.StatusChanged,
                "Status",
                oldStatus.ToString(),
                newStatus.ToString());

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issue status changed successfully. IssueId: {IssueId}, From: {OldStatus}, To: {NewStatus}, UserId: {UserId}",
                issueId,
                oldStatus,
                newStatus,
                currentUserId);
        }

        public async Task ChangeStoryPointsAsync(
            Guid issueId,
            int? storyPoints)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot change story points because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            var oldStoryPoints = issue.StoryPoints;

            issue.StoryPoints = storyPoints;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issue story points changed. IssueId: {IssueId}, OldValue: {OldStoryPoints}, NewValue: {NewStoryPoints}",
                issueId,
                oldStoryPoints,
                storyPoints);
        }

        private static bool IsValidTransition(
            IssueStatus currentStatus,
            IssueStatus newStatus)
        {
            return
                (currentStatus == IssueStatus.Todo &&
                 newStatus == IssueStatus.InProgress)

                || (currentStatus == IssueStatus.InProgress &&
                    newStatus == IssueStatus.InReview)

                || (currentStatus == IssueStatus.InReview &&
                    newStatus == IssueStatus.Done)

                || (currentStatus == IssueStatus.Done &&
                    newStatus == IssueStatus.Todo);
        }

        public async Task AssignAsync(
            Guid issueId,
            Guid userId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot assign issue because it was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            var currentUserId = _currentUserService.UserId;

            var targetMember =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        userId);

            if (targetMember == null)
            {
                _logger.LogWarning(
                    "Attempt to assign issue to non-member. IssueId: {IssueId}, UserId: {UserId}",
                    issueId,
                    userId);

                throw new BusinessRuleException(
                    "L'utilisateur à assigner doit être membre du projet.");
            }

            if (issue.AssigneeId == userId)
            {
                _logger.LogDebug(
                    "Issue is already assigned to the specified user. IssueId: {IssueId}, UserId: {UserId}",
                    issueId,
                    userId);

                throw new BusinessRuleException(
                    "Cette issue est déjà assignée à cet utilisateur.");
            }

            var oldAssigneeId = issue.AssigneeId;

            issue.AssigneeId = userId;

            await _activityLogService.LogAsync(
                issue.Id,
                currentUserId,
                ActivityAction.Assigned,
                "Assignee",
                oldAssigneeId?.ToString(),
                userId.ToString());

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issue assigned successfully. IssueId: {IssueId}, OldAssigneeId: {OldAssigneeId}, NewAssigneeId: {NewAssigneeId}, UserId: {UserId}",
                issueId,
                oldAssigneeId,
                userId,
                currentUserId);
        }

        public async Task MoveToSprintAsync(
            Guid issueId,
            Guid? sprintId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot move issue because it was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            var currentUserId = _currentUserService.UserId;
            var previousSprintId = issue.SprintId;

            if (!sprintId.HasValue)
            {
                if (issue.SprintId == null)
                {
                    _logger.LogDebug(
                        "Issue is already in backlog. IssueId: {IssueId}",
                        issueId);

                    throw new BusinessRuleException(
                        "L'issue est déjà dans le backlog.");
                }

                issue.SprintId = null;
            }
            else
            {
                var sprint = await _unitOfWork.Sprints
                    .GetByIdAsync(sprintId.Value);

                if (sprint == null)
                {
                    _logger.LogWarning(
                        "Sprint not found while moving issue. SprintId: {SprintId}",
                        sprintId.Value);

                    throw new NotFoundException(
                        "Sprint non trouvé.");
                }

                if (sprint.ProjectId != issue.ProjectId)
                {
                    _logger.LogWarning(
                        "Sprint does not belong to issue project. IssueId: {IssueId}, SprintId: {SprintId}",
                        issueId,
                        sprintId.Value);

                    throw new BusinessRuleException(
                        "Le sprint n'appartient pas au même projet que l'issue.");
                }

                if (sprint.Status == SprintStatus.Completed)
                {
                    _logger.LogWarning(
                        "Attempt to move issue to completed sprint. IssueId: {IssueId}, SprintId: {SprintId}",
                        issueId,
                        sprintId.Value);

                    throw new BusinessRuleException(
                        "Impossible de déplacer une issue vers un sprint terminé.");
                }

                if (issue.SprintId == sprintId)
                {
                    _logger.LogDebug(
                        "Issue already belongs to sprint. IssueId: {IssueId}, SprintId: {SprintId}",
                        issueId,
                        sprintId.Value);

                    throw new BusinessRuleException(
                        "L'issue appartient déjà à ce sprint.");
                }

                issue.SprintId = sprintId;
            }

            await _activityLogService.LogAsync(
                issue.Id,
                currentUserId,
                ActivityAction.SprintChanged,
                "Sprint",
                previousSprintId?.ToString(),
                issue.SprintId?.ToString());

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issue sprint changed. IssueId: {IssueId}, OldSprintId: {OldSprintId}, NewSprintId: {NewSprintId}, UserId: {UserId}",
                issueId,
                previousSprintId,
                issue.SprintId,
                currentUserId);
        }

        public async Task MoveToEpicAsync(
            Guid issueId,
            Guid? epicId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot move issue to Epic because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            if (epicId.HasValue)
            {
                await ValidateEpicAsync(
                    issue.ProjectId,
                    epicId.Value);
            }

            var previousEpicId = issue.EpicId;

            issue.EpicId = epicId;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issue Epic changed. IssueId: {IssueId}, OldEpicId: {OldEpicId}, NewEpicId: {NewEpicId}",
                issueId,
                previousEpicId,
                epicId);
        }

        public async Task ReorderAsync(
            IEnumerable<ReorderIssueItemDto> items)
        {
            var reorderItems = items.ToList();

            if (reorderItems.Count == 0)
            {
                _logger.LogWarning(
                    "Reorder requested with an empty issue list.");

                throw new BusinessRuleException(
                    "La liste des Issues à réordonner est vide.");
            }

            if (reorderItems
                .GroupBy(x => x.IssueId)
                .Any(g => g.Count() > 1))
            {
                _logger.LogWarning(
                    "Reorder request contains duplicate issue IDs.");

                throw new BusinessRuleException(
                    "Une même Issue ne peut pas apparaître plusieurs fois.");
            }

            var issueIds = reorderItems
                .Select(x => x.IssueId)
                .ToList();

            var issues = (await _unitOfWork.Issues
                .GetByIdsAsync(issueIds))
                .ToList();

            if (issues.Count != issueIds.Count)
            {
                _logger.LogWarning(
                    "Reorder request contains one or more unknown issues.");

                throw new NotFoundException(
                    "Une ou plusieurs Issues sont introuvables.");
            }

            if (issues
                .Select(i => i.ProjectId)
                .Distinct()
                .Count() > 1)
            {
                _logger.LogWarning(
                    "Reorder request contains issues from multiple projects.");

                throw new BusinessRuleException(
                    "Les Issues à réordonner doivent appartenir au même projet.");
            }

            foreach (var item in reorderItems)
            {
                var issue = issues.First(
                    i => i.Id == item.IssueId);

                issue.DisplayOrder = item.DisplayOrder;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issues reordered successfully. IssueCount: {IssueCount}",
                reorderItems.Count);
        }

        public async Task DeleteAsync(
            Guid issueId)
        {
            var currentUserId = _currentUserService.UserId;

            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot delete issue because it was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            _unitOfWork.Issues.Delete(issue);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Issue deleted successfully. IssueId: {IssueId}, UserId: {UserId}",
                issueId,
                currentUserId);
        }
    }
}