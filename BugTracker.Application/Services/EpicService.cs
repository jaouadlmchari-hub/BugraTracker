using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class EpicService : IEpicService
    {
        private readonly IUnitOfWork _unitOfWork;

        public EpicService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<EpicDto?> GetByIdAsync(Guid epicId)
        {
            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
                return null;

            return epic.ToDto();
        }

        public async Task<EpicDetailsDto?> GetByIdWithDetailsAsync(Guid epicId)
        {
            var epic = await _unitOfWork.Epics
                .GetByIdWithDetailsAsync(epicId);

            if (epic == null)
                return null;

            return epic.ToDetailsDto();
        }

        public async Task<IEnumerable<EpicDto>> GetAllByProjectAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet introuvable.");

            var epics = await _unitOfWork.Epics
                .GetByProjectIdAsync(projectId);

            return epics
                .Select(e => e.ToDto())
                .ToList();
        }

        public async Task<IEnumerable<EpicDto>> GetActiveByProjectAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet introuvable.");

            var epics = await _unitOfWork.Epics
                .GetActiveEpicsAsync(projectId);

            return epics
                .Select(e => e.ToDto())
                .ToList();
        }

        public async Task<EpicDto> CreateAsync(Guid projectId, CreateEpicDto dto)
        {
            // 1. Vérifier que le projet existe
            var projectExists = await _unitOfWork.Projects
                .ExistsAsync(projectId);

            if (!projectExists)
                throw new NotFoundException(
                    "Projet non trouvé.");


            // 2. Créer l'Epic
            var epic = new Epic
            {
                ProjectId = projectId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                ColorCode = dto.ColorCode,
                Status = EpicStatus.Active
            };

            await _unitOfWork.Epics.AddAsync(epic);

            // 3. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 4. Retourner le DTO
            return epic.ToDto();
        }

        public async Task<EpicDto> UpdateAsync(Guid epicId, UpdateEpicDto dto)
        {
            // 1. Récupérer l'Epic
            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
                throw new NotFoundException(
                    "Epic non trouvé.");

            // 2. Règle métier :
            // un Epic archivé ne peut plus être modifié
            if (epic.Status == EpicStatus.Archived)
            {
                throw new BusinessRuleException(
                    "Impossible de modifier un Epic archivé.");
            }


            // 3. Modifier
            epic.Title = dto.Title.Trim();
            epic.Description = dto.Description?.Trim();
            epic.ColorCode = dto.ColorCode;

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            return epic.ToDto();
        }

        public async Task DeleteAsync(Guid epicId)
        {
            // 1. Charger l'Epic avec ses Issues
            var epic = await _unitOfWork.Epics
                .GetByIdWithDetailsAsync(epicId);

            if (epic == null)
                throw new NotFoundException(
                    "Epic non trouvé.");

            // 2. Détacher les Issues
            foreach (var issue in epic.Issues)
            {
                issue.EpicId = null;
            }

            // 3. Supprimer l'Epic
            _unitOfWork.Epics.Delete(epic);

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(Guid epicId, EpicStatus newStatus)
        {
            // 1. Récupérer l'Epic
            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
                throw new NotFoundException(
                    "Epic non trouvé.");

            // 2. Vérifier la valeur de l'enum
            if (!Enum.IsDefined(typeof(EpicStatus), newStatus))
            {
                throw new BusinessRuleException(
                    "Le statut fourni est invalide.");
            }


            // 3. Modifier le statut
            epic.Status = newStatus;

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}