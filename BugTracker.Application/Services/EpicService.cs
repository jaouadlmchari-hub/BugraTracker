using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Epics;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services
{
    public class EpicService : IEpicService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly ILogger<EpicService> _logger;

        public EpicService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            ILogger<EpicService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<EpicDto?> GetByIdAsync(Guid epicId)
        {
            _logger.LogDebug(
                "Retrieving epic {EpicId}.",
                epicId);

            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
            {
                _logger.LogWarning(
                    "Epic {EpicId} was not found.",
                    epicId);

                return null;
            }

            return epic.ToDto();
        }

        public async Task<EpicDetailsDto?> GetByIdWithDetailsAsync(Guid epicId)
        {
            _logger.LogDebug(
                "Retrieving epic {EpicId} with details.",
                epicId);

            var epic = await _unitOfWork.Epics
                .GetByIdWithDetailsAsync(epicId);

            if (epic == null)
            {
                _logger.LogWarning(
                    "Epic {EpicId} was not found.",
                    epicId);

                return null;
            }

            return epic.ToDetailsDto();
        }

        public async Task<IEnumerable<EpicDto>> GetAllByProjectAsync(Guid projectId)
        {
            _logger.LogDebug(
                "Retrieving all epics for project {ProjectId}.",
                projectId);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Cannot retrieve epics. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException(
                    "Projet introuvable.");
            }

            var epics = await _unitOfWork.Epics
                .GetByProjectIdAsync(projectId);

            _logger.LogDebug(
                "Retrieved {EpicCount} epics for project {ProjectId}.",
                epics.Count(),
                projectId);

            return epics
                .Select(e => e.ToDto())
                .ToList();
        }

        public async Task<IEnumerable<EpicDto>> GetActiveByProjectAsync(Guid projectId)
        {
            _logger.LogDebug(
                "Retrieving active epics for project {ProjectId}.",
                projectId);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Cannot retrieve active epics. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException(
                    "Projet introuvable.");
            }

            var epics = await _unitOfWork.Epics
                .GetActiveEpicsAsync(projectId);

            _logger.LogDebug(
                "Retrieved {EpicCount} active epics for project {ProjectId}.",
                epics.Count(),
                projectId);

            return epics
                .Select(e => e.ToDto())
                .ToList();
        }

        public async Task<EpicDto> CreateAsync(Guid projectId,CreateEpicDto dto)
        {
            _logger.LogInformation(
                "Creating epic {EpicTitle} in project {ProjectId}.",
                dto.Title,
                projectId);

            var projectExists = await _unitOfWork.Projects
                .ExistsAsync(projectId);

            if (!projectExists)
            {
                _logger.LogWarning(
                    "Epic creation failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException(
                    "Projet non trouvé.");
            }

            var epic = new Epic
            {
                ProjectId = projectId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                ColorCode = dto.ColorCode,
                Status = EpicStatus.Active
            };

            await _unitOfWork.Epics.AddAsync(epic);

            // Audit : EpicCreated
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.EpicCreated,
                EntityName: nameof(Epic),
                EntityId: epic.Id.ToString(),
                OldValue: null,
                NewValue: $"Title: {epic.Title}, ColorCode: {epic.ColorCode}, Status: {epic.Status}, ProjectId: {projectId}",
                Details: $"Création de l'Epic '{epic.Title}' dans le projet {projectId}"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Epic {EpicId} created successfully in project {ProjectId}.",
                epic.Id,
                projectId);

            return epic.ToDto();
        }

        public async Task<EpicDto> UpdateAsync(Guid epicId,UpdateEpicDto dto)
        {
            _logger.LogInformation(
                "Updating epic {EpicId}.",
                epicId);

            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
            {
                _logger.LogWarning(
                    "Epic update failed. Epic {EpicId} was not found.",
                    epicId);

                throw new NotFoundException(
                    "Epic non trouvé.");
            }

            if (epic.Status == EpicStatus.Archived)
            {
                _logger.LogWarning(
                    "Epic update rejected. Epic {EpicId} is archived.",
                    epicId);

                throw new BusinessRuleException(
                    "Impossible de modifier un Epic archivé.");
            }

            var oldValues = $"Title: {epic.Title}, Description: {epic.Description}, ColorCode: {epic.ColorCode}";

            epic.Title = dto.Title.Trim();
            epic.Description = dto.Description?.Trim();
            epic.ColorCode = dto.ColorCode;

            var newValues = $"Title: {epic.Title}, Description: {epic.Description}, ColorCode: {epic.ColorCode}";

            // Audit : EpicUpdated
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.EpicUpdated,
                EntityName: nameof(Epic),
                EntityId: epic.Id.ToString(),
                OldValue: oldValues,
                NewValue: newValues,
                Details: $"Mise à jour de l'Epic '{epic.Title}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Epic {EpicId} updated successfully.",
                epicId);

            return epic.ToDto();
        }

        public async Task DeleteAsync(Guid epicId)
        {
            _logger.LogInformation(
                "Deleting epic {EpicId}.",
                epicId);

            var epic = await _unitOfWork.Epics
                .GetByIdWithDetailsAsync(epicId);

            if (epic == null)
            {
                _logger.LogWarning(
                    "Epic deletion failed. Epic {EpicId} was not found.",
                    epicId);

                throw new NotFoundException(
                    "Epic non trouvé.");
            }

            var detachedIssueCount = epic.Issues.Count;
            var oldValues = $"Title: {epic.Title}, ColorCode: {epic.ColorCode}, Status: {epic.Status}, ProjectId: {epic.ProjectId}";

            foreach (var issue in epic.Issues)
            {
                issue.EpicId = null;
            }

            _unitOfWork.Epics.Delete(epic);

            // Audit : EpicDeleted
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.EpicDeleted,
                EntityName: nameof(Epic),
                EntityId: epic.Id.ToString(),
                OldValue: oldValues,
                NewValue: null,
                Details: $"Suppression de l'Epic '{epic.Title}' ({detachedIssueCount} ticket(s) détaché(s))"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Epic {EpicId} deleted successfully. " +
                "{IssueCount} issues were detached.",
                epicId,
                detachedIssueCount);
        }

        public async Task ChangeStatusAsync(Guid epicId,EpicStatus newStatus)
        {
            _logger.LogInformation(
                "Changing status of epic {EpicId} to {NewStatus}.",
                epicId,
                newStatus);

            var epic = await _unitOfWork.Epics
                .GetByIdAsync(epicId);

            if (epic == null)
            {
                _logger.LogWarning(
                    "Change status failed. Epic {EpicId} was not found.",
                    epicId);

                throw new NotFoundException(
                    "Epic non trouvé.");
            }

            if (!Enum.IsDefined(typeof(EpicStatus), newStatus))
            {
                _logger.LogWarning(
                    "Change status rejected. Invalid status {NewStatus} for epic {EpicId}.",
                    newStatus,
                    epicId);

                throw new BusinessRuleException(
                    "Le statut fourni est invalide.");
            }

            var oldStatus = epic.Status;

            epic.Status = newStatus;

            // Audit : EpicStatusChanged (ou EpicUpdated selon votre enum AuditAction)
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.EpicUpdated,
                EntityName: nameof(Epic),
                EntityId: epic.Id.ToString(),
                OldValue: $"Status: {oldStatus}",
                NewValue: $"Status: {newStatus}",
                Details: $"Changement du statut de l'Epic '{epic.Title}' de '{oldStatus}' vers '{newStatus}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Epic {EpicId} status changed from {OldStatus} to {NewStatus}.",
                epicId,
                oldStatus,
                newStatus);
        }
    }
}