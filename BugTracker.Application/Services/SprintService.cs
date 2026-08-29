using BugTracker.Application.DTOs.Sprints;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class SprintService : ISprintService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SprintService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SprintDto?> GetByIdAsync(Guid sprintId)
        {
            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
                return null;

            return sprint.ToDto();
        }

        public async Task<IEnumerable<SprintDto>> GetAllByProjectAsync(Guid projectId)
        {
            var sprints = await _unitOfWork.Sprints
                .GetByProjectIdAsync(projectId);

            return sprints
                .Select(s => s.ToDto())
                .ToList();
        }

        public async Task<SprintDto> CreateAsync(Guid projectId, CreateSprintDto dto)
        {
            // 1. Vérifier que le projet existe
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet non trouvé.");


            // 2. Vérifier les dates
            if (dto.StartDate.HasValue &&
                dto.EndDate.HasValue &&
                dto.EndDate <= dto.StartDate)
            {
                throw new BusinessRuleException(
                    "La date de fin doit être postérieure à la date de début.");
            }

            // 3. Créer le sprint
            var sprint = new Sprint
            {
                ProjectId = projectId,
                Name = dto.Name,
                Goal = dto.Goal,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = SprintStatus.Planning
            };

            // 4. Ajouter
            await _unitOfWork.Sprints.AddAsync(sprint);

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            return sprint.ToDto();
        }

        public async Task<SprintDto> UpdateAsync(Guid sprintId, UpdateSprintDto dto)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException(
                    "Sprint non trouvé.");

            // 2. Règle métier :
            // seul un Sprint Planning peut être modifié
            if (sprint.Status != SprintStatus.Planning)
            {
                throw new BusinessRuleException(
                    "Seul un sprint en Planning peut être modifié.");
            }

            // 3. Vérifier les dates
            if (dto.StartDate.HasValue &&
                dto.EndDate.HasValue &&
                dto.EndDate <= dto.StartDate)
            {
                throw new BusinessRuleException(
                    "La date de fin doit être postérieure à la date de début.");
            }

            // 4. Modifier
            sprint.Name = dto.Name;
            sprint.Goal = dto.Goal;
            sprint.StartDate = dto.StartDate;
            sprint.EndDate = dto.EndDate;

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            return sprint.ToDto();
        }

        public async Task StartAsync(Guid sprintId)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException(
                    "Sprint non trouvé.");

            // 2. Le Sprint doit être en Planning
            if (sprint.Status != SprintStatus.Planning)
            {
                throw new BusinessRuleException(
                    "Seul un sprint en Planning peut être démarré.");
            }


            // 3. Un seul Sprint actif par projet
            var activeSprints = await _unitOfWork.Sprints
                .GetActiveSprintsAsync(sprint.ProjectId);

            if (activeSprints.Any())
            {
                throw new BusinessRuleException(
                    "Un sprint est déjà actif pour ce projet.");
            }

            // 4. Démarrer
            sprint.Status = SprintStatus.Active;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteAsync(Guid sprintId)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException(
                    "Sprint non trouvé.");

            // 2. Le Sprint doit être actif
            if (sprint.Status != SprintStatus.Active)
            {
                throw new BusinessRuleException(
                    "Seul un sprint actif peut être terminé.");
            }

            // 3. Récupérer les Issues non terminées
            var unfinishedIssues = await _unitOfWork.Issues
                .GetUnfinishedBySprintIdAsync(sprintId);

            // 4. Remettre les Issues non terminées dans le Backlog
            foreach (var issue in unfinishedIssues)
            {
                issue.SprintId = null;
            }

            // 5. Terminer le Sprint
            sprint.Status = SprintStatus.Completed;
            sprint.CompletedAt = DateTime.UtcNow;

            // 6. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid sprintId)
        {
            // 1. Récupérer le sprint
            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
                throw new NotFoundException(
                    "Sprint non trouvé.");


            // 2. Un Sprint actif ne peut pas être supprimé
            if (sprint.Status == SprintStatus.Active)
            {
                throw new BusinessRuleException(
                    "Un sprint actif ne peut pas être supprimé.");
            }

            // 3. Supprimer
            _unitOfWork.Sprints.Delete(sprint);

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}