using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Projects;
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
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            ILogger<ProjectService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ProjectDto?> GetByIdAsync(Guid projectId)
        {
            _logger.LogDebug(
                "Retrieving project {ProjectId}.",
                projectId);

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project {ProjectId} was not found.",
                    projectId);

                return null;
            }

            return project.ToDto();
        }

        public async Task<ProjectDto?> GetByKeyAsync(string key)
        {
            key = key.Trim().ToUpperInvariant();

            _logger.LogDebug(
                "Retrieving project with key {ProjectKey}.",
                key);

            var project = await _unitOfWork.Projects.GetByKeyAsync(key);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project with key {ProjectKey} was not found.",
                    key);

                return null;
            }

            return project.ToDto();
        }

        public async Task<PagedResultDto<ProjectDto>> GetAllPaginatedAsync(ProjectFilterDto filter)
        {
            var userId = _currentUserService.UserId;
            var isAdmin = _currentUserService.IsAdmin;

            _logger.LogDebug(
                "Retrieving projects. UserId: {UserId}, IsAdmin: {IsAdmin}, Page: {PageNumber}, PageSize: {PageSize}.",
                userId,
                isAdmin,
                filter.PageNumber,
                filter.PageSize);

            var (projects, totalCount) =
                await _unitOfWork.Projects.GetPaginatedAsync(
                    filter,
                    userId,
                    isAdmin);

            _logger.LogDebug(
                "Retrieved {ProjectCount} projects out of {TotalCount}.",
                projects.Count(),
                totalCount);

            return new PagedResultDto<ProjectDto>
            {
                Items = projects.Select(p => p.ToDto()).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
        {
            var ownerId = _currentUserService.UserId;

            _logger.LogInformation(
                "Creating project {ProjectName} with key {ProjectKey} by user {UserId}.",
                dto.Name,
                dto.Key,
                ownerId);

            var existingProject =
                await _unitOfWork.Projects.GetByKeyAsync(dto.Key);

            if (existingProject != null)
            {
                _logger.LogWarning(
                    "Project creation failed. Project key {ProjectKey} is already in use.",
                    dto.Key);

                throw new ConflictException(
                    "La clé du projet est déjà utilisée.");
            }

            var project = new Project
            {
                Name = dto.Name,
                Key = dto.Key,
                Description = dto.Description,
                OwnerId = ownerId,
                Status = ProjectStatus.Active
            };

            project.Members.Add(new ProjectMember
            {
                UserId = ownerId,
                Role = ProjectRole.Manager
            });

            await _unitOfWork.Projects.AddAsync(project);

            // Audit : ProjectCreated
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: ownerId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectCreated,
                EntityName: nameof(Project),
                EntityId: project.Id.ToString(),
                OldValue: null,
                NewValue: $"Name: {project.Name}, Key: {project.Key}, Status: {project.Status}",
                Details: $"Création du projet '{project.Name}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Project {ProjectId} created successfully with key {ProjectKey} by user {UserId}.",
                project.Id,
                project.Key,
                ownerId);

            return project.ToDto();
        }

        public async Task<ProjectDto> UpdateAsync(Guid projectId, UpdateProjectDto dto)
        {
            _logger.LogInformation(
                "Updating project {ProjectId}.",
                projectId);

            var project =
                await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project update failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            var oldValues = $"Name: {project.Name}, Description: {project.Description}";

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.UpdatedAt = DateTime.UtcNow;

            var newValues = $"Name: {project.Name}, Description: {project.Description}";

            // Audit : ProjectUpdated
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectUpdated,
                EntityName: nameof(Project),
                EntityId: project.Id.ToString(),
                OldValue: oldValues,
                NewValue: newValues,
                Details: $"Mise à jour des informations du projet '{project.Name}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Project {ProjectId} updated successfully.",
                projectId);

            return project.ToDto();
        }

        public async Task ArchiveAsync(Guid projectId)
        {
            _logger.LogInformation(
                "Archiving project {ProjectId}.",
                projectId);

            var project =
                await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project archive failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            if (project.Status == ProjectStatus.Archived)
            {
                _logger.LogWarning(
                    "Project {ProjectId} is already archived.",
                    projectId);

                throw new BusinessRuleException(
                    "Le projet est déjà archivé.");
            }

            var oldStatus = project.Status.ToString();

            project.Status = ProjectStatus.Archived;
            project.UpdatedAt = DateTime.UtcNow;

            // Audit : ProjectArchived
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectArchived,
                EntityName: nameof(Project),
                EntityId: project.Id.ToString(),
                OldValue: $"Status: {oldStatus}",
                NewValue: $"Status: {project.Status}",
                Details: $"Archivage du projet '{project.Name}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Project {ProjectId} archived successfully.",
                projectId);
        }

        public async Task ActivateAsync(Guid projectId)
        {
            _logger.LogInformation(
                "Activating project {ProjectId}.",
                projectId);

            var project =
                await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project activation failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            if (project.Status == ProjectStatus.Active)
            {
                _logger.LogWarning(
                    "Project {ProjectId} is already active.",
                    projectId);

                throw new BusinessRuleException(
                    "Le projet est déjà actif.");
            }

            var oldStatus = project.Status.ToString();

            project.Status = ProjectStatus.Active;
            project.UpdatedAt = DateTime.UtcNow;

            // Audit : ProjectActivated
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectActivated,
                EntityName: nameof(Project),
                EntityId: project.Id.ToString(),
                OldValue: $"Status: {oldStatus}",
                NewValue: $"Status: {project.Status}",
                Details: $"Réactivation du projet '{project.Name}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Project {ProjectId} activated successfully.",
                projectId);
        }

        public async Task ChangeOwnerAsync(Guid projectId, Guid newOwnerId)
        {
            _logger.LogInformation(
                "Changing owner of project {ProjectId} to user {NewOwnerId}.",
                projectId,
                newOwnerId);

            var project =
                await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Change owner failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            var newOwner =
                await _unitOfWork.Users.GetByIdAsync(newOwnerId);

            if (newOwner == null)
            {
                _logger.LogWarning(
                    "Change owner failed. User {NewOwnerId} was not found.",
                    newOwnerId);

                throw new NotFoundException(
                    "Le nouvel utilisateur n'existe pas.");
            }

            if (!newOwner.IsActive)
            {
                _logger.LogWarning(
                    "Change owner failed. User {NewOwnerId} is inactive.",
                    newOwnerId);

                throw new BusinessRuleException(
                    "Le nouvel utilisateur est désactivé.");
            }

            if (project.OwnerId == newOwnerId)
            {
                _logger.LogWarning(
                    "Change owner failed. User {NewOwnerId} is already the owner of project {ProjectId}.",
                    newOwnerId,
                    projectId);

                throw new BusinessRuleException(
                    "Cet utilisateur est déjà propriétaire du projet.");
            }

            var oldOwnerId = project.OwnerId;

            project.OwnerId = newOwnerId;
            project.UpdatedAt = DateTime.UtcNow;

            // Audit : ProjectOwnerChanged
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectOwnerChanged,
                EntityName: nameof(Project),
                EntityId: project.Id.ToString(),
                OldValue: $"OwnerId: {oldOwnerId}",
                NewValue: $"OwnerId: {newOwnerId}",
                Details: $"Changement du propriétaire du projet '{project.Name}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Owner of project {ProjectId} changed successfully to user {NewOwnerId}.",
                projectId,
                newOwnerId);
        }

        public async Task DeleteAsync(Guid projectId)
        {
            _logger.LogInformation(
                "Deleting project {ProjectId}.",
                projectId);

            var project =
                await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Project deletion failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            var oldValues = $"Name: {project.Name}, Key: {project.Key}, Status: {project.Status}, OwnerId: {project.OwnerId}";

            _unitOfWork.Projects.Delete(project);

            // Audit : ProjectDeleted
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectDeleted,
                EntityName: nameof(Project),
                EntityId: project.Id.ToString(),
                OldValue: oldValues,
                NewValue: null,
                Details: $"Suppression du projet '{project.Name}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Project {ProjectId} deleted successfully.",
                projectId);
        }
    }
}