using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class IssueService : IIssueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IActivityLogService _activityLogService;

        public IssueService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IActivityLogService activityLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _activityLogService = activityLogService;
        }

        public async Task<IssueDto?> GetByIdAsync(Guid issueId)
        {
            var issue = await _unitOfWork.Issues.GetByIdWithDetailsAsync(issueId);
            if (issue == null)
                return null;

            return issue.ToDto();
        }

        public async Task<PagedResultDto<IssueDto>> GetByProjectPaginatedAsync(
            Guid projectId, IssueFilterDto filter)
        {
            var projectExists =
                await _unitOfWork.Projects.ExistsAsync(projectId);

            if (!projectExists)
                throw new NotFoundException("Projet introuvable.");

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


        private async Task ValidateEpicAsync(Guid projectId, Guid epicId)
        {
            var epic = await _unitOfWork.Epics.GetByIdAsync(epicId);

            if (epic == null)
                throw new NotFoundException("Epic non trouvé.");

            if (epic.ProjectId != projectId)
                throw new BusinessRuleException(
                    "L'Epic n'appartient pas au même projet que l'Issue.");

            if (epic.Status == EpicStatus.Archived)
                throw new BusinessRuleException(
                    "Impossible d'affecter une Issue à un Epic archivé.");
        }

        public async Task<IssueDto> CreateAsync(Guid projectId, CreateIssueDto dto)
        {
            await using var transaction =
                await _unitOfWork.BeginTransactionAsync();

            try
            {
                var project = await _unitOfWork.Projects
                    .GetByIdAsync(projectId);

                if (project == null)
                    throw new NotFoundException("Projet non trouvé.");

                var currentUserId = _currentUserService.UserId;

                if (dto.SprintId.HasValue)
                {
                    var sprint = await _unitOfWork.Sprints
                        .GetByIdAsync(dto.SprintId.Value);

                    if (sprint == null)
                        throw new NotFoundException("Sprint non trouvé.");

                    if (sprint.ProjectId != projectId)
                        throw new BusinessRuleException(
                            "Le sprint n'appartient pas à ce projet.");
                }

                if (dto.EpicId.HasValue)
                {
                    await ValidateEpicAsync(projectId, dto.EpicId.Value);
                }

                if (dto.AssigneeId.HasValue)
                {
                    var assignee = await _unitOfWork.ProjectMembers
                        .GetByProjectAndUserAsync(
                            projectId,
                            dto.AssigneeId.Value);

                    if (assignee == null)
                        throw new BusinessRuleException(
                            "L'utilisateur assigné doit être membre du projet.");
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

                // Premier SaveChanges :
                // SQL Server génère l'Id avec NEWID()
                await _unitOfWork.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    issue.Id,
                    currentUserId,
                    ActivityAction.Created);

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                var createdIssue =
                    await _unitOfWork.Issues.GetByIdWithDetailsAsync(issue.Id);

                if (createdIssue == null)
                    throw new NotFoundException(
                        "Impossible de récupérer l'issue créée.");

                return createdIssue.ToDto();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IssueDto> UpdateAsync(Guid issueId, UpdateIssueDto dto)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            // 2. Modifier les propriétés
            issue.Title = dto.Title;
            issue.Description = dto.Description;
            issue.Type = dto.Type;
            issue.Priority = dto.Priority;
            issue.StoryPoints = dto.StoryPoints;
            issue.DueDate = dto.DueDate;

            if (dto.EpicId != issue.EpicId)
            {
                if (dto.EpicId.HasValue)
                {
                    await ValidateEpicAsync(issue.ProjectId, dto.EpicId.Value);
                }

                issue.EpicId = dto.EpicId;
            }

            // 3. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 4. Recharger les navigations nécessaires au mapping
            var updatedIssue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (updatedIssue == null)
                throw new NotFoundException(
                    "Impossible de récupérer l'issue modifiée.");

            return updatedIssue.ToDto();
        }

        public async Task ChangeStatusAsync(Guid issueId, IssueStatus newStatus)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            // 2. Vérifier que le nouveau statut est valide
            if (!Enum.IsDefined(typeof(IssueStatus), newStatus))
            {
                throw new BusinessRuleException(
                    "Le statut fourni est invalide.");
            }

            // 3. Vérifier si l'Issue appartient à un sprint terminé
            if (issue.SprintId.HasValue)
            {
                var sprint = await _unitOfWork.Sprints
                    .GetByIdAsync(issue.SprintId.Value);

                if (sprint != null &&
                    sprint.Status == SprintStatus.Completed)
                {
                    throw new BusinessRuleException(
                        "Une issue appartenant à un sprint terminé ne peut plus changer de statut.");
                }
            }

            var currentUserId = _currentUserService.UserId;

            // 4. Règle spéciale : Done → Todo
            // L'autorisation générale est gérée par CanChangeIssueStatus,
            // mais la réouverture reste limitée aux QA (sur Bug), PM et Admin.
            if (issue.Status == IssueStatus.Done &&
                newStatus == IssueStatus.Todo &&
                !_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                var canReopen =
                    currentMember != null &&
                    (
                        currentMember.Role == ProjectRole.Manager ||
                        (currentMember.Role == ProjectRole.QA &&
                         issue.Type == IssueType.Bug)
                    );

                if (!canReopen)
                    throw new ForbiddenException(
                        "Seuls les QA, PM et Admin peuvent rouvrir cette issue.");
            }

            // 5. Vérifier la transition
            if (!IsValidTransition(issue.Status, newStatus))
            {
                throw new BusinessRuleException(
                    "INVALID_STATUS_TRANSITION");
            }

            // 6. Conserver l'ancien statut
            var oldStatus = issue.Status;

            // 7. Modifier le statut
            issue.Status = newStatus;

            // 8. ActivityLog
            await _activityLogService.LogAsync(
                    issue.Id,
                    currentUserId,
                    ActivityAction.StatusChanged,
                    "Status",
                    oldStatus.ToString(),
                    newStatus.ToString());

            // 9. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeStoryPointsAsync(Guid issueId, int? storyPoints)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");


            issue.StoryPoints = storyPoints;

            await _unitOfWork.SaveChangesAsync();
        }

        private static bool IsValidTransition(IssueStatus currentStatus, IssueStatus newStatus)
        {
            return (currentStatus == IssueStatus.Todo &&
                    newStatus == IssueStatus.InProgress)

                || (currentStatus == IssueStatus.InProgress &&
                    newStatus == IssueStatus.InReview)

                || (currentStatus == IssueStatus.InReview &&
                    newStatus == IssueStatus.Done)

                || (currentStatus == IssueStatus.Done &&
                    newStatus == IssueStatus.Todo);
        }

        public async Task AssignAsync(Guid issueId, Guid userId)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            // 2. Utilisateur courant nécessaire pour l'ActivityLog
            var currentUserId = _currentUserService.UserId;

            // 3. Vérifier que l'utilisateur cible est membre du projet
            var targetMember = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(
                    issue.ProjectId,
                    userId);

            if (targetMember == null)
            {
                throw new BusinessRuleException(
                    "L'utilisateur à assigner doit être membre du projet.");
            }

            // 4. Vérifier si l'utilisateur est déjà assigné
            if (issue.AssigneeId == userId)
            {
                throw new BusinessRuleException(
                    "Cette issue est déjà assignée à cet utilisateur.");
            }

            // 5. Garder l'ancien assigné pour ActivityLog
            var oldAssigneeId = issue.AssigneeId;

            // 6. Assigner l'utilisateur
            issue.AssigneeId = userId;

            // 7. ActivityLog
            await _activityLogService.LogAsync(
                issue.Id,
                currentUserId,
                ActivityAction.Assigned,
                "Assignee",
                oldAssigneeId?.ToString(),
                userId.ToString());

            // 8. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MoveToSprintAsync(Guid issueId, Guid? sprintId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            var currentUserId = _currentUserService.UserId;

            var previousSprintId = issue.SprintId;

            if (!sprintId.HasValue)
            {
                if (issue.SprintId == null)
                {
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
                    throw new NotFoundException("Sprint non trouvé.");

                if (sprint.ProjectId != issue.ProjectId)
                {
                    throw new BusinessRuleException(
                        "Le sprint n'appartient pas au même projet que l'issue.");
                }

                if (sprint.Status == SprintStatus.Completed)
                {
                    throw new BusinessRuleException(
                        "Impossible de déplacer une issue vers un sprint terminé.");
                }

                if (issue.SprintId == sprintId)
                {
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
        }

        public async Task MoveToEpicAsync(Guid issueId, Guid? epicId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            if (epicId.HasValue)
            {
                await ValidateEpicAsync(
                    issue.ProjectId,
                    epicId.Value);
            }

            issue.EpicId = epicId;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReorderAsync(IEnumerable<ReorderIssueItemDto> items)
        {
            var reorderItems = items.ToList();

            if (reorderItems.Count == 0)
                throw new BusinessRuleException(
                    "La liste des Issues à réordonner est vide.");

            // Vérifier qu'un IssueId n'est pas envoyé plusieurs fois
            if (reorderItems
                .GroupBy(x => x.IssueId)
                .Any(g => g.Count() > 1))
            {
                throw new BusinessRuleException(
                    "Une même Issue ne peut pas apparaître plusieurs fois.");
            }

            // Récupérer toutes les Issues en une seule requête
            var issueIds = reorderItems
                .Select(x => x.IssueId)
                .ToList();

            var issues = (await _unitOfWork.Issues
                .GetByIdsAsync(issueIds))
                .ToList();

            // Vérifier que toutes existent
            if (issues.Count != issueIds.Count)
                throw new NotFoundException(
                    "Une ou plusieurs Issues sont introuvables.");

            // Pour l'instant, un reorder concerne un seul projet
            if (issues.Select(i => i.ProjectId).Distinct().Count() > 1)
                throw new BusinessRuleException(
                    "Les Issues à réordonner doivent appartenir au même projet.");

            foreach (var item in reorderItems)
            {
                var issue = issues.First(i => i.Id == item.IssueId);

                issue.DisplayOrder = item.DisplayOrder;
            }

            // Une seule sauvegarde pour toutes les Issues
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid issueId)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            // 2. Supprimer l'Issue
            _unitOfWork.Issues.Delete(issue);

            // 3. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}