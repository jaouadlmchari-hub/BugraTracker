

using BugTracker.Application.DTOs.Issues;
using BugTracker.Application.Interfaces;
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

        public IssueService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, 
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

        public async Task<IEnumerable<IssueDto>> GetByProjectAsync(Guid projectId)
        {
            var issues = await _unitOfWork.Issues
                .GetByProjectIdAsync(projectId);

            return issues
                .Select(i => i.ToDto())
                .ToList();
        }

        public async Task<IssueDto> CreateAsync(Guid projectId,CreateIssueDto dto)
        {
            // 1. Vérifier que le projet existe
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            // 2. Vérifier que l'utilisateur connecté est membre du projet
            var currentUserId = _currentUserService.UserId;

            var currentMember = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(projectId, currentUserId);

            if (currentMember == null)
                throw new UnauthorizedAccessException(
                    "Vous devez être membre du projet pour créer un ticket.");

            // 3. Si un Sprint est fourni, vérifier qu'il existe
            //    et qu'il appartient au même projet
            Sprint? sprint = null;

            if (dto.SprintId.HasValue)
            {
                sprint = await _unitOfWork.Sprints
                    .GetByIdAsync(dto.SprintId.Value);

                if (sprint == null)
                    throw new KeyNotFoundException("Sprint non trouvé.");

                if (sprint.ProjectId != projectId)
                    throw new InvalidOperationException(
                        "Le sprint n'appartient pas à ce projet.");
            }

            // 4. Si un Assignee est fourni, vérifier qu'il est membre
            //    du projet
            if (dto.AssigneeId.HasValue)
            {
                var assignee = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        dto.AssigneeId.Value);

                if (assignee == null)
                    throw new InvalidOperationException(
                        "L'utilisateur assigné doit être membre du projet.");
            }

            // 5. Créer l'Issue
            var issue = new Issue
            {
                ProjectId = projectId,

                Title = dto.Title,
                Description = dto.Description,

                Type = dto.Type,

                // ISSUE-02
                Priority = dto.Priority ?? Priority.Medium,

                // ISSUE-03
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

            // 6. Créer le ActivityLog "created"
            await _activityLogService.LogAsync(issue.Id, currentUserId, ActivityAction.Created);

            await _unitOfWork.SaveChangesAsync();

            // 8. Charger les navigations nécessaires au mapping
            var createdIssue = await _unitOfWork.Issues.GetByIdWithDetailsAsync(issue.Id);

            if (createdIssue == null)
                throw new InvalidOperationException(
                    "Impossible de récupérer l'issue créée.");

            return createdIssue.ToDto();
        }

        public async Task<IssueDto> UpdateAsync(Guid issueId,UpdateIssueDto dto)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
                throw new KeyNotFoundException("Issue non trouvée.");

            // 2. Vérifier les droits de modification
            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                if (currentMember == null)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'êtes pas membre de ce projet.");
                }

                var canUpdate =
                    issue.ReporterId == currentUserId ||
                    issue.AssigneeId == currentUserId ||
                    currentMember.Role == ProjectRole.Manager;

                if (!canUpdate)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour modifier cette issue.");
                }
            }

            // 3. Modifier les propriétés
            issue.Title = dto.Title;
            issue.Description = dto.Description;
            issue.Type = dto.Type;
            issue.Priority = dto.Priority;
            issue.StoryPoints = dto.StoryPoints;
            issue.DueDate = dto.DueDate;
            issue.EpicId = dto.EpicId;

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 5. Recharger les navigations nécessaires au mapping
            var updatedIssue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (updatedIssue == null)
                throw new InvalidOperationException(
                    "Impossible de récupérer l'issue modifiée.");

            return updatedIssue.ToDto();
        }

        public async Task ChangeStatusAsync(Guid issueId, IssueStatus newStatus)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdWithDetailsAsync(issueId);

            if (issue == null)
                throw new KeyNotFoundException("Issue non trouvée.");

            // 2. Vérifier que le nouveau statut est valide
            if (!Enum.IsDefined(typeof(IssueStatus), newStatus))
            {
                throw new InvalidOperationException(
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
                    throw new InvalidOperationException(
                        "Une issue appartenant à un sprint terminé ne peut plus changer de statut.");
                }
            }

            // 4. Vérifier les droits de l'utilisateur
            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                if (currentMember == null)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'êtes pas membre de ce projet.");
                }
                var isManager = currentMember.Role == ProjectRole.Manager;
                var isQA = currentMember.Role == ProjectRole.QA;
                var isAssignee = issue.AssigneeId == currentUserId;
                var isQABug = isQA && issue.Type == IssueType.Bug;

                // Réouverture : Done → Todo
                if (issue.Status == IssueStatus.Done &&
                    newStatus == IssueStatus.Todo)
                {
                    if (!isQABug && !isManager)
                    {
                        throw new UnauthorizedAccessException(
                            "Seuls les QA, PM et Admin peuvent rouvrir une issue.");
                    }
                }
                // Workflow normal
                else
                {
                    if (!isAssignee && !isManager && !isQABug)
                    {
                        throw new UnauthorizedAccessException(
                            "Vous n'avez pas les droits pour changer le statut de cette issue.");
                    }
                }
            }

            // 5. Vérifier la transition
            if (!IsValidTransition(issue.Status, newStatus))
            {
                throw new InvalidOperationException(
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

        private static bool IsValidTransition(IssueStatus currentStatus,IssueStatus newStatus)
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
                throw new KeyNotFoundException("Issue non trouvée.");

            // 2. Vérifier les droits de l'utilisateur connecté
            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls les PM et Admin peuvent assigner une issue.");
                }
            }

            // 3. Vérifier que l'utilisateur cible est membre du projet
            var targetMember = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(
                    issue.ProjectId,
                    userId);

            if (targetMember == null)
            {
                throw new InvalidOperationException(
                    "L'utilisateur à assigner doit être membre du projet.");
            }

            // 4. Vérifier si l'utilisateur est déjà assigné
            if (issue.AssigneeId == userId)
            {
                throw new InvalidOperationException(
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
                throw new KeyNotFoundException("Issue non trouvée.");

            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls les PM et Admin peuvent déplacer une issue vers un sprint.");
                }
            }

            var previousSprintId = issue.SprintId;

            if (!sprintId.HasValue)
            {
                if (issue.SprintId == null)
                {
                    throw new InvalidOperationException(
                        "L'issue est déjà dans le backlog.");
                }

                issue.SprintId = null;
            }
            else
            {
                var sprint = await _unitOfWork.Sprints
                    .GetByIdAsync(sprintId.Value);

                if (sprint == null)
                    throw new KeyNotFoundException("Sprint non trouvé.");

                if (sprint.ProjectId != issue.ProjectId)
                {
                    throw new InvalidOperationException(
                        "Le sprint n'appartient pas au même projet que l'issue.");
                }

                if (sprint.Status == SprintStatus.Completed)
                {
                    throw new InvalidOperationException(
                        "Impossible de déplacer une issue vers un sprint terminé.");
                }

                if (issue.SprintId == sprintId)
                {
                    throw new InvalidOperationException(
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

        public async Task DeleteAsync(Guid issueId)
        {
            // 1. Récupérer l'Issue
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new KeyNotFoundException("Issue non trouvée.");

            // 2. Vérifier les droits
            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls les PM et Admin peuvent supprimer une issue.");
                }
            }

            // 3. Supprimer l'Issue
            _unitOfWork.Issues.Delete(issue);

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
