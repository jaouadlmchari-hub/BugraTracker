using BugTracker.Application.DTOs.Sprints;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class SprintService : ISprintService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public SprintService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<SprintDto?> GetByIdAsync(Guid sprintId)
        {
            var sprint = await _unitOfWork.Sprints.GetByIdAsync(sprintId);

            if (sprint == null)
                return null;

            return sprint.ToDto();
        }

        public async Task<IEnumerable<SprintDto>> GetAllByProjectAsync(Guid projectId)
        {
            var sprints = await _unitOfWork.Sprints.GetByProjectIdAsync(projectId);

            return sprints
                .Select(s => s.ToDto())
                .ToList();
        }

        public async Task<SprintDto> CreateAsync(Guid projectId, CreateSprintDto dto)
        {
            // 1. Vérifier que le projet existe
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException("Projet non trouvé.");

            // 2. Vérifier les droits
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new ForbiddenException(
                        "Vous n'avez pas les droits pour créer un sprint.");
                }
            }

            // 3. Vérifier les dates
            if (dto.StartDate.HasValue &&
                dto.EndDate.HasValue &&
                dto.EndDate <= dto.StartDate)
            {
                throw new BusinessRuleException(
                    "La date de fin doit être postérieure à la date de début.");
            }

            // 4. Créer le sprint
            var sprint = new Sprint
            {
                ProjectId = projectId,
                Name = dto.Name,
                Goal = dto.Goal,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = SprintStatus.Planning
            };

            // 5. Ajouter
            await _unitOfWork.Sprints.AddAsync(sprint);

            // 6. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 7. Retourner le DTO
            return sprint.ToDto();
        }

        public async Task<SprintDto> UpdateAsync(Guid sprintId, UpdateSprintDto dto)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints.GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException("Sprint non trouvé.");

            // 2. Vérifier le statut
            if (sprint.Status != SprintStatus.Planning)
            {
                throw new BusinessRuleException(
                    "Seul un sprint en Planning peut être modifié.");
            }

            // 3. Vérifier les droits
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        sprint.ProjectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new ForbiddenException(
                        "Vous n'avez pas les droits pour modifier ce sprint.");
                }
            }

            // 4. Vérifier les dates
            if (dto.StartDate.HasValue &&
                dto.EndDate.HasValue &&
                dto.EndDate <= dto.StartDate)
            {
                throw new BusinessRuleException(
                    "La date de fin doit être postérieure à la date de début.");
            }

            // 5. Modifier les propriétés autorisées
            sprint.Name = dto.Name;
            sprint.Goal = dto.Goal;
            sprint.StartDate = dto.StartDate;
            sprint.EndDate = dto.EndDate;

            // 6. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            return sprint.ToDto();
        }

        public async Task StartAsync(Guid sprintId)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints.GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException("Sprint non trouvé.");

            // 2. Vérifier que le sprint est en Planning
            if (sprint.Status != SprintStatus.Planning)
            {
                throw new BusinessRuleException(
                    "Seul un sprint en Planning peut être démarré.");
            }

            // 3. Vérifier les droits
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        sprint.ProjectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new ForbiddenException(
                        "Vous n'avez pas les droits pour démarrer ce sprint.");
                }
            }

            // 4. Vérifier qu'il n'existe pas déjà un sprint Active
            var activeSprints = await _unitOfWork.Sprints
                .GetActiveSprintsAsync(sprint.ProjectId);

            if (activeSprints.Any())
            {
                throw new BusinessRuleException(
                    "Un sprint est déjà actif pour ce projet.");
            }

            // 5. Démarrer le sprint
            sprint.Status = SprintStatus.Active;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteAsync(Guid sprintId)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints.GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException("Sprint non trouvé.");

            // 2. Vérifier les droits
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        sprint.ProjectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new ForbiddenException(
                        "Vous n'avez pas les droits pour terminer ce sprint.");
                }
            }

            // 3. Le sprint doit être Active
            if (sprint.Status != SprintStatus.Active)
            {
                throw new BusinessRuleException(
                    "Seul un sprint actif peut être terminé.");
            }

            // 4. Récupérer les tickets non terminés
            var unfinishedIssues = await _unitOfWork.Issues
                .GetUnfinishedBySprintIdAsync(sprintId);

            // 5. Remettre ces tickets dans le backlog
            foreach (var issue in unfinishedIssues)
            {
                issue.SprintId = null;
            }

            // 6. Terminer le sprint
            sprint.Status = SprintStatus.Completed;
            sprint.CompletedAt = DateTime.UtcNow;

            // 7. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid sprintId)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints.GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException("Sprint non trouvé.");

            // 2. Vérifier les droits
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        sprint.ProjectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new ForbiddenException(
                        "Vous n'avez pas les droits pour supprimer ce sprint.");
                }
            }

            // 3. Un sprint Active ne peut pas être supprimé
            if (sprint.Status == SprintStatus.Active)
            {
                throw new BusinessRuleException(
                    "Un sprint actif ne peut pas être supprimé.");
            }

            // 4. Supprimer le sprint
            _unitOfWork.Sprints.Delete(sprint);

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}