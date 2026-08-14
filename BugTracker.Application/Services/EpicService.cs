using BugTracker.Domain.Enums;
using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using System.Runtime.CompilerServices;
using BugTracker.Application.Mappings;

namespace BugTracker.Application.Services
{
    public class EpicService :IEpicService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public EpicService(ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
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

        public async Task<EpicDto> CreateAsync(Guid projectId, CreateEpicDto dto)
        {
            // 1. Vérifier que le projet existe
            var projectExists = await _unitOfWork.Projects.ExistsAsync(projectId);

            if (!projectExists)
                throw new KeyNotFoundException("Projet non trouvé.");

            // 2. Utilisateur connecté
            var currentUserId = _currentUserService.UserId;

            // 3. Vérifier les permissions
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls le Manager et l'Admin peuvent créer un Epic.");
                }
            }

            // 4. Créer l'Epic
            var epic = new Epic
            {
                ProjectId = projectId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                ColorCode = dto.ColorCode,
                Status = EpicStatus.Active
            };

            await _unitOfWork.Epics.AddAsync(epic);

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 6. Retourner le DTO
            return epic.ToDto();
        }

        public async Task<EpicDto> UpdateAsync(Guid epicId, UpdateEpicDto dto)
        {
            // 1. Récupérer l'Epic
            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
                throw new KeyNotFoundException(
                    "Epic non trouvé.");

            var currentUserId = _currentUserService.UserId;

            // 2. Vérifier les permissions
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        epic.ProjectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls le Manager et l'Admin peuvent modifier un Epic.");
                }
            }

            // 3. Mettre à jour les propriétés
            epic.Title = dto.Title.Trim();
            epic.Description = dto.Description?.Trim();
            epic.ColorCode = dto.ColorCode;

            // 4. Sauvegarder
            
            await _unitOfWork.SaveChangesAsync();

            // 5. Retourner le DTO
            return epic.ToDto();
        }

        public async Task DeleteAsync(Guid epicId)
        {
            // 1. Récupérer l'Epic avec ses Issues
            var epic = await _unitOfWork.Epics
                .GetByIdWithDetailsAsync(epicId);

            if (epic == null)
                throw new KeyNotFoundException(
                    "Epic non trouvé.");

            var currentUserId = _currentUserService.UserId;

            // 2. Vérifier les permissions
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        epic.ProjectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls le Manager et l'Admin peuvent supprimer un Epic.");
                }
            }

            // 3. Détacher les Issues de l'Epic
            foreach (var issue in epic.Issues)
            {
                issue.EpicId = null;
            }

            // 4. Supprimer l'Epic
            _unitOfWork.Epics.Delete(epic);

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(Guid epicId, EpicStatus newStatus)
        {
            // 1. Récupérer l'Epic
            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
                throw new KeyNotFoundException(
                    "Epic non trouvé.");

            var currentUserId = _currentUserService.UserId;

            // 2. Vérifier les permissions
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        epic.ProjectId,
                        currentUserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls le Manager et l'Admin peuvent modifier le statut d'un Epic.");
                }
            }

            // 3. Modifier le statut
            epic.Status = newStatus;

            // 4. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
