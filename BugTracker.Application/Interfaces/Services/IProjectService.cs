using BugTracker.Application.DTOs.Common;
using BugTracker.Application.DTOs.Projects;

namespace BugTracker.Application.Interfaces;

public interface IProjectService
{
    // --- Consultation ---
    Task<ProjectDto?> GetByIdAsync(Guid projectId);
    Task<ProjectDto?> GetByKeyAsync(string key);
    Task<PagedResultDto<ProjectDto>> GetAllPaginatedAsync(ProjectFilterDto filter);

    // --- Création & Modification de base ---
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateAsync(Guid projectId, UpdateProjectDto dto);

    // --- Actions Métier & Cycle de vie ---
    Task ArchiveAsync(Guid projectId);
    Task ActivateAsync(Guid projectId);
    Task ChangeOwnerAsync(Guid projectId, Guid newOwnerId);

    // --- Suppression ---
    Task DeleteAsync(Guid projectId);
}