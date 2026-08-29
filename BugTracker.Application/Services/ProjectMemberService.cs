using BugTracker.Application.DTOs.ProjectMembers;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProjectMemberService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet non trouvé.");


            var members = await _unitOfWork.ProjectMembers
                .GetByProjectIdAsync(projectId);

            return members
                .Select(m => m.ToDto())
                .ToList();
        }

        public async Task<ProjectMemberDto?> GetMemberAsync(Guid projectId, Guid userId)
        {
            var member = await _unitOfWork.ProjectMembers
                .GetByProjectAndUserAsync(projectId, userId);

            return member?.ToDto();
        }

        public async Task<bool> ShareAnyProjectAsync(Guid firstUserId,  Guid secondUserId)
        {
            return await _unitOfWork.ProjectMembers
                .ShareAnyProjectAsync(
                    firstUserId,
                    secondUserId);
        }

        public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto dto)
        {
            // 1. Vérifier que le projet existe
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet non trouvé.");


            // 2. Projet archivé
            if (project.Status == ProjectStatus.Archived)
            {
                throw new BusinessRuleException(
                    "Un projet archivé ne peut plus recevoir de nouveaux membres.");
            }

            // 3. Vérifier que l'utilisateur existe
            var user = await _unitOfWork.Users
                .GetByIdAsync(dto.UserId);

            if (user == null)
                throw new NotFoundException(
                    "Utilisateur non trouvé.");

            // 4. L'utilisateur doit être actif
            if (!user.IsActive)
            {
                throw new BusinessRuleException(
                    "Impossible d'ajouter un utilisateur désactivé.");
            }

            // 5. Ne pas ajouter deux fois le même membre
            var existingMember =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        dto.UserId);

            if (existingMember != null)
            {
                throw new BusinessRuleException(
                    "Cet utilisateur est déjà membre de ce projet.");
            }

            // 6. Créer le membre
            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = dto.UserId,
                Role = dto.Role
            };

            await _unitOfWork.ProjectMembers
                .AddAsync(projectMember);

            // 7. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // Nécessaire au mapping Username / FullName
            projectMember.User = user;

            return projectMember.ToDto();
        }

        public async Task ChangeRoleAsync(Guid projectId, Guid userId, ProjectRole newRole)
        {
            // 1. Vérifier le projet
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet non trouvé.");

            // 2. Projet archivé
            if (project.Status == ProjectStatus.Archived)
            {
                throw new BusinessRuleException(
                    "Un projet archivé ne peut plus être modifié.");
            }


            // 3. Récupérer le membre cible
            var memberToUpdate =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        userId);

            if (memberToUpdate == null)
            {
                throw new NotFoundException(
                    "Cet utilisateur n'est pas membre de ce projet.");
            }

            // 4. Même rôle
            if (memberToUpdate.Role == newRole)
            {
                throw new BusinessRuleException(
                    "L'utilisateur possède déjà ce rôle.");
            }

            // 5. Le projet doit toujours avoir au moins un Manager
            if (memberToUpdate.Role == ProjectRole.Manager &&
                newRole != ProjectRole.Manager)
            {
                var managerCount =
                    await _unitOfWork.ProjectMembers
                        .CountManagersAsync(projectId);

                if (managerCount <= 1)
                {
                    throw new BusinessRuleException(
                        "Impossible de modifier le rôle. " +
                        "Le projet doit conserver au moins un Manager.");
                }
            }

            // 6. Modifier
            memberToUpdate.Role = newRole;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            // 1. Vérifier le projet
            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
                throw new NotFoundException(
                    "Projet non trouvé.");

            // 2. Projet archivé
            if (project.Status == ProjectStatus.Archived)
            {
                throw new BusinessRuleException(
                    "Un projet archivé ne peut plus être modifié.");
            }


            // 3. Récupérer le membre cible
            var memberToRemove =
                await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        projectId,
                        userId);

            if (memberToRemove == null)
            {
                throw new NotFoundException(
                    "Cet utilisateur n'est pas membre de ce projet.");
            }

            // 4. Le Owner ne peut pas être retiré directement
            if (project.OwnerId == userId)
            {
                throw new BusinessRuleException(
                    "Impossible de retirer le propriétaire du projet. " +
                    "Transférez d'abord la propriété du projet.");
            }

            // 5. Toujours conserver au moins un Manager
            if (memberToRemove.Role == ProjectRole.Manager)
            {
                var managerCount =
                    await _unitOfWork.ProjectMembers
                        .CountManagersAsync(projectId);

                if (managerCount <= 1)
                {
                    throw new BusinessRuleException(
                        "Impossible de retirer le dernier Manager du projet.");
                }
            }

            // 6. Retirer ses assignations aux Issues
            var assignedIssues =
                await _unitOfWork.Issues
                    .GetByProjectAndAssigneeAsync(
                        projectId,
                        userId);

            foreach (var issue in assignedIssues)
            {
                issue.AssigneeId = null;
            }

            // 7. Supprimer le membre
            _unitOfWork.ProjectMembers.Delete(
                memberToRemove);

            // 8. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> IsMemberAsync(Guid projectId, Guid userId)
        {
            return await _unitOfWork.ProjectMembers
                .IsMemberAsync(projectId, userId);
        }

        public async Task<bool> IsManagerAsync(Guid projectId, Guid userId)
        {
            return await _unitOfWork.ProjectMembers
                .IsManagerAsync(projectId, userId);
        }
    }
}