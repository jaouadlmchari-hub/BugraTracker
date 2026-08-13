using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Projects;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public  class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUserService;
        public ProjectService(IUnitOfWork unitOfWork , ICurrentUserService currentUserService) 
        {
            _unitOfWork = unitOfWork;

            _currentUserService = currentUserService;
        }

        public async Task<ProjectDto?> GetByIdAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
                return null;

            return project.ToDto();
        }

        public async Task<ProjectDto?> GetByKeyAsync(string key)
        {
            key = key.Trim().ToUpperInvariant();

            var project = await _unitOfWork.Projects.GetByKeyAsync(key);

            if (project == null)
                return null;

            return project.ToDto();
        }

        public async Task<PagedResultDto<ProjectDto>> GetAllPaginatedAsync(ProjectFilterDto filter)
        {
            var userId = _currentUserService.UserId;
            var isAdmin = _currentUserService.IsAdmin;

            var (projects, totalCount) =
                await _unitOfWork.Projects.GetPaginatedAsync(
                    filter,
                    userId,
                    isAdmin);

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

            var existingProject = await _unitOfWork.Projects
                .GetByKeyAsync(dto.Key);

            if (existingProject != null)
                throw new InvalidOperationException("La clé du projet est déjà utilisée.");

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

            await _unitOfWork.SaveChangesAsync();

            return project.ToDto();
        }

        public async Task<ProjectDto> UpdateAsync(Guid projectId,UpdateProjectDto dto)
        {
            var project = await _unitOfWork.Projects.GetByIdWithMembersAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

          

            if (!_currentUserService.IsAdmin)
            {
                var member = project.Members
                           .FirstOrDefault(m => m.UserId == _currentUserService.UserId);

                if (member == null || member.Role != ProjectRole.Manager)
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour modifier ce projet.");

            
            }

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return project.ToDto();
        }

        public async Task ArchiveAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects.GetByIdWithMembersAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            if (!_currentUserService.IsAdmin)
            {
                var member = project.Members
                    .FirstOrDefault(m => m.UserId == _currentUserService.UserId);

                if (member == null || member.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour archiver ce projet.");
                }
            }

            if (project.Status == ProjectStatus.Archived)
                throw new InvalidOperationException(
                    "Le projet est déjà archivé.");

            project.Status = ProjectStatus.Archived;
            project.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ActivateAsync(Guid projectId)
        {
            var project = await _unitOfWork.Projects
                .GetByIdWithMembersAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            if (!_currentUserService.IsAdmin)
            {
                var member = project.Members
                    .FirstOrDefault(m => m.UserId == _currentUserService.UserId);

                if (member == null || member.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour activer ce projet.");
                }
            }

            if (project.Status == ProjectStatus.Active)
                throw new InvalidOperationException(
                    "Le projet est déjà actif.");

            project.Status = ProjectStatus.Active;
            project.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeOwnerAsync(Guid projectId, Guid newOwnerId)
        {
            var project = await _unitOfWork.Projects.GetByIdWithMembersAsync(projectId);

            if (project == null)
                throw new KeyNotFoundException("Projet non trouvé.");

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = project.Members
                    .FirstOrDefault(m => m.UserId == _currentUserService.UserId);

                if (currentMember == null ||
                    currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'avez pas les droits pour changer le propriétaire.");
                }
            }

            var newOwner = await _unitOfWork.Users.GetByIdAsync(newOwnerId);

            if (newOwner == null)
                throw new KeyNotFoundException(
                    "Le nouvel utilisateur n'existe pas.");

            if (!newOwner.IsActive)
                throw new InvalidOperationException(
                    "Le nouvel utilisateur est désactivé.");

            if (project.OwnerId == newOwnerId)
                throw new InvalidOperationException(
                    "Cet utilisateur est déjà propriétaire du projet.");

            project.OwnerId = newOwnerId;
            project.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid projectId)
        {
            if (!_currentUserService.IsAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Seul un administrateur peut supprimer un projet.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project == null)
            {
                throw new KeyNotFoundException("Projet non trouvé.");
            }

            _unitOfWork.Projects.Delete(project);

            await _unitOfWork.SaveChangesAsync();
        }

    }
}
