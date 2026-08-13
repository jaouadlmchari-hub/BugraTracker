using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUserService;

        public ProjectMemberService(IUnitOfWork unitOfWork ,ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            var members = await _unitOfWork.ProjectMembers
                .GetByProjectIdAsync(projectId);

            if (!_currentUserService.IsAdmin)
            {
                var isMember = members.Any(
                    m => m.UserId == _currentUserService.UserId);

                if (!isMember)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas accès aux membres de ce projet.");
                }
            }

            return members
                .Select(m => m.ToDto())
                .ToList();
        }

        public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId,AddProjectMemberDto dto)
        {
          
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            // 2. Vérifier les permissions
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour ajouter un membre à ce projet.");
                }
            }

            // 3. Un projet archivé ne peut plus recevoir de membres
            if (project.Status == ProjectStatus.Archived)
            {
                throw new InvalidOperationException(
                    "Un projet archivé ne peut plus recevoir de nouveaux membres.");
            }

            // 4. Vérifier que l'utilisateur existe
            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);

            if (user == null)
                throw new KeyNotFoundException("Utilisateur non trouvé.");

            // 5. Vérifier que l'utilisateur est actif
            if (!user.IsActive)
            {
                throw new InvalidOperationException(
                    "Impossible d'ajouter un utilisateur désactivé.");
            }

            // 6. Vérifier qu'il n'est pas déjà membre
            var existingMember = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(projectId, dto.UserId);

            if (existingMember != null)
            {
                throw new InvalidOperationException(
                    "Cet utilisateur est déjà membre de ce projet.");
            }

            // 7. Création du membre
            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = dto.UserId,
                Role = dto.Role
            };

            await _unitOfWork.ProjectMembers.AddAsync(projectMember);

            // 8. Sauvegarde
            await _unitOfWork.SaveChangesAsync();

            // Pour le mapping Username / FullName
            projectMember.User = user;

            return projectMember.ToDto();
        }

        public async Task ChangeRoleAsync(Guid projectId, Guid userId, ProjectRole newRole)
        {
            // 1. Await indispensable pour obtenir l'objet Project
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            if (project.Status == ProjectStatus.Archived)
            {
                throw new InvalidOperationException("Un projet archivé ne peut plus être modifié.");
            }

            // 2. Vérification des droits de l'utilisateur CONNECTÉ
            if (!_currentUserService.IsAdmin)
            {
                var currentUserId = _currentUserService.UserId;
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(projectId, currentUserId);

                if (currentMember == null || currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour modifier le rôle d'un membre.");
                }
            }

            // 3. Récupération du membre CIBLE
            var memberToUpdate = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(projectId, userId);

            if (memberToUpdate == null)
                throw new KeyNotFoundException("Cet utilisateur n'est pas membre de ce projet.");

            if (memberToUpdate.Role == newRole)
                throw new InvalidOperationException("L'utilisateur possède déjà ce rôle.");

            // 4. Règle métier : Empêcher de rétrograder le dernier Manager
            if (memberToUpdate.Role == ProjectRole.Manager && newRole != ProjectRole.Manager)
            {
                var managerCount = await _unitOfWork.ProjectMembers.CountManagersAsync(projectId);

                if (managerCount <= 1)
                {
                    throw new InvalidOperationException(
                        "Impossible de modifier le rôle. Le projet doit conserver au moins un Manager.");
                }
            }

            // 5. Mise à jour
            memberToUpdate.Role = newRole;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            // 1. Vérifier que le projet existe
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            // 2. Un projet archivé ne peut plus être modifié
            if (project.Status == ProjectStatus.Archived)
            {
                throw new InvalidOperationException(
                    "Un projet archivé ne peut plus être modifié.");
            }

            // 3. Vérifier les droits de l'utilisateur connecté
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour retirer un membre.");
                }
            }

            // 4. Récupérer le membre à retirer
            var memberToRemove = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(projectId, userId);

            if (memberToRemove == null)
            {
                throw new KeyNotFoundException(
                    "Cet utilisateur n'est pas membre de ce projet.");
            }

            // 5. Empêcher la suppression du dernier Manager
            if (memberToRemove.Role == ProjectRole.Manager)
            {
                var managerCount =
                    await _unitOfWork.ProjectMembers.CountManagersAsync(projectId);

                if (managerCount <= 1)
                {
                    throw new InvalidOperationException(
                        "Impossible de retirer le dernier Manager du projet.");
                }
            }

            // 6. Supprimer l'assignation aux tickets
            var assignedIssues = await _unitOfWork.Issues
                .GetByProjectAndAssigneeAsync(projectId, userId);

            foreach (var issue in assignedIssues)
            {
                issue.AssigneeId = null;
            }

            // 7. Supprimer le membre
            _unitOfWork.ProjectMembers.Delete(memberToRemove);

            // 8. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
