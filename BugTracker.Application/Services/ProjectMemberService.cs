using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.ProjectMembers;
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
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProjectMemberService> _logger;

        public ProjectMemberService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            ILogger<ProjectMemberService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId)
        {
            _logger.LogDebug(
                "Retrieving members for project {ProjectId}.",
                projectId);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Cannot retrieve members. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            var members = await _unitOfWork.ProjectMembers
                .GetByProjectIdAsync(projectId);

            _logger.LogDebug(
                "Retrieved {MemberCount} members for project {ProjectId}.",
                members.Count(),
                projectId);

            return members
                .Select(m => m.ToDto())
                .ToList();
        }

        public async Task<ProjectMemberDto?> GetMemberAsync(Guid projectId, Guid userId)
        {
            _logger.LogDebug(
                "Retrieving member {UserId} from project {ProjectId}.",
                userId,
                projectId);

            var member = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(projectId, userId);

            if (member == null)
            {
                _logger.LogDebug(
                    "User {UserId} is not a member of project {ProjectId}.",
                    userId,
                    projectId);
            }

            return member?.ToDto();
        }

        public async Task<bool> ShareAnyProjectAsync(Guid firstUserId, Guid secondUserId)
        {
            _logger.LogDebug(
                "Checking whether users {FirstUserId} and {SecondUserId} share a project.",
                firstUserId,
                secondUserId);

            return await _unitOfWork.ProjectMembers
                .ShareAnyProjectAsync(
                    firstUserId,
                    secondUserId);
        }

        public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto dto)
        {
            _logger.LogInformation(
                "Adding user {UserId} to project {ProjectId} with role {Role}.",
                dto.UserId,
                projectId,
                dto.Role);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Add member failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException("Projet non trouvé.");
            }

            if (project.Status == ProjectStatus.Archived)
            {
                _logger.LogWarning(
                    "Add member rejected. Project {ProjectId} is archived.",
                    projectId);

                throw new BusinessRuleException(
                    "Un projet archivé ne peut plus recevoir de nouveaux membres.");
            }

            var user = await _unitOfWork.Users
                .GetByIdAsync(dto.UserId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Add member failed. User {UserId} was not found.",
                    dto.UserId);

                throw new NotFoundException(
                    "Utilisateur non trouvé.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Add member rejected. User {UserId} is inactive.",
                    dto.UserId);

                throw new BusinessRuleException(
                    "Impossible d'ajouter un utilisateur désactivé.");
            }

            var existingMember =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        dto.UserId);

            if (existingMember != null)
            {
                _logger.LogWarning(
                    "Add member rejected. User {UserId} is already a member of project {ProjectId}.",
                    dto.UserId,
                    projectId);

                throw new BusinessRuleException(
                    "Cet utilisateur est déjà membre de ce projet.");
            }

            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = dto.UserId,
                Role = dto.Role
            };

            await _unitOfWork.ProjectMembers
                .AddAsync(projectMember);

            // Audit : ProjectMemberAdded
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectMemberAdded,
                EntityName: nameof(ProjectMember),
                EntityId: $"{projectId}:{dto.UserId}",
                OldValue: null,
                NewValue: $"ProjectId: {projectId}, UserId: {dto.UserId}, Role: {dto.Role}",
                Details: $"Ajout de l'utilisateur '{user.Email}' au projet '{project.Name}' avec le rôle '{dto.Role}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            projectMember.User = user;

            _logger.LogInformation(
                "User {UserId} successfully added to project {ProjectId} with role {Role}.",
                dto.UserId,
                projectId,
                dto.Role);

            return projectMember.ToDto();
        }

        public async Task ChangeRoleAsync(Guid projectId,Guid userId,ProjectRole newRole)
        {
            _logger.LogInformation(
                "Changing role of user {UserId} in project {ProjectId} to {NewRole}.",
                userId,
                projectId,
                newRole);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Change role failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException(
                    "Projet non trouvé.");
            }

            if (project.Status == ProjectStatus.Archived)
            {
                _logger.LogWarning(
                    "Change role rejected. Project {ProjectId} is archived.",
                    projectId);

                throw new BusinessRuleException(
                    "Un projet archivé ne peut plus être modifié.");
            }

            var memberToUpdate =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        userId);

            if (memberToUpdate == null)
            {
                _logger.LogWarning(
                    "Change role failed. User {UserId} is not a member of project {ProjectId}.",
                    userId,
                    projectId);

                throw new NotFoundException(
                    "Cet utilisateur n'est pas membre de ce projet.");
            }

            if (memberToUpdate.Role == newRole)
            {
                _logger.LogWarning(
                    "Change role rejected. User {UserId} already has role {Role} in project {ProjectId}.",
                    userId,
                    newRole,
                    projectId);

                throw new BusinessRuleException(
                    "L'utilisateur possède déjà ce rôle.");
            }

            if (memberToUpdate.Role == ProjectRole.Manager &&
                newRole != ProjectRole.Manager)
            {
                var managerCount =
                    await _unitOfWork.ProjectMembers
                        .CountManagersAsync(projectId);

                if (managerCount <= 1)
                {
                    _logger.LogWarning(
                        "Change role rejected. User {UserId} is the last Manager of project {ProjectId}.",
                        userId,
                        projectId);

                    throw new BusinessRuleException(
                        "Impossible de modifier le rôle. " +
                        "Le projet doit conserver au moins un Manager.");
                }
            }

            var oldRole = memberToUpdate.Role;

            memberToUpdate.Role = newRole;

            // Audit : ProjectMemberRoleChanged
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectMemberRoleChanged,
                EntityName: nameof(ProjectMember),
                EntityId: $"{projectId}:{userId}",
                OldValue: $"Role: {oldRole}",
                NewValue: $"Role: {newRole}",
                Details: $"Changement du rôle du membre {userId} dans le projet '{project.Name}' de '{oldRole}' vers '{newRole}'"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Role of user {UserId} in project {ProjectId} changed from {OldRole} to {NewRole}.",
                userId,
                projectId,
                oldRole,
                newRole);
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            _logger.LogInformation(
                "Removing user {UserId} from project {ProjectId}.",
                userId,
                projectId);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Remove member failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException(
                    "Projet non trouvé.");
            }

            if (project.Status == ProjectStatus.Archived)
            {
                _logger.LogWarning(
                    "Remove member rejected. Project {ProjectId} is archived.",
                    projectId);

                throw new BusinessRuleException(
                    "Un projet archivé ne peut plus être modifié.");
            }

            var memberToRemove =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        userId);

            if (memberToRemove == null)
            {
                _logger.LogWarning(
                    "Remove member failed. User {UserId} is not a member of project {ProjectId}.",
                    userId,
                    projectId);

                throw new NotFoundException(
                    "Cet utilisateur n'est pas membre de ce projet.");
            }

            if (project.OwnerId == userId)
            {
                _logger.LogWarning(
                    "Remove member rejected. User {UserId} is the owner of project {ProjectId}.",
                    userId,
                    projectId);

                throw new BusinessRuleException(
                    "Impossible de retirer le propriétaire du projet. " +
                    "Transférez d'abord la propriété du projet.");
            }

            if (memberToRemove.Role == ProjectRole.Manager)
            {
                var managerCount =
                    await _unitOfWork.ProjectMembers
                        .CountManagersAsync(projectId);

                if (managerCount <= 1)
                {
                    _logger.LogWarning(
                        "Remove member rejected. User {UserId} is the last Manager of project {ProjectId}.",
                        userId,
                        projectId);

                    throw new BusinessRuleException(
                        "Impossible de retirer le dernier Manager du projet.");
                }
            }

            var assignedIssues =
                await _unitOfWork.Issues
                    .GetByProjectAndAssigneeAsync(
                        projectId,
                        userId);

            var unassignedIssueCount = assignedIssues.Count();

            foreach (var issue in assignedIssues)
            {
                issue.AssigneeId = null;
            }

            var oldRole = memberToRemove.Role;

            _unitOfWork.ProjectMembers.Delete(
                memberToRemove);

            // Audit : ProjectMemberRemoved
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: _currentUserService.UserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.ProjectMemberRemoved,
                EntityName: nameof(ProjectMember),
                EntityId: $"{projectId}:{userId}",
                OldValue: $"ProjectId: {projectId}, UserId: {userId}, Role: {oldRole}",
                NewValue: null,
                Details: $"Retrait de l'utilisateur {userId} du projet '{project.Name}' ({unassignedIssueCount} ticket(s) réaffecté(s))"
            ));

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} successfully removed from project {ProjectId}. " +
                "{IssueCount} issue assignments were cleared.",
                userId,
                projectId,
                unassignedIssueCount);
        }

        public async Task<bool> IsMemberAsync(Guid projectId, Guid userId)
        {
            return await _unitOfWork.ProjectMembers
                .IsMemberAsync(projectId, userId);
        }

        public async Task<bool> IsManagerAsync(Guid projectId,Guid userId)
        {
            return await _unitOfWork.ProjectMembers
                .IsManagerAsync(projectId, userId);
        }
    }
}