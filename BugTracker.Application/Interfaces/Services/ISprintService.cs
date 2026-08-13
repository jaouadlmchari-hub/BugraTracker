using BugTracker.Application.DTOs.Sprints;

namespace BugTracker.Application.Interfaces.Services
{
    public interface ISprintService
    {
        Task<SprintDto?> GetByIdAsync(Guid sprintId);

        Task<IEnumerable<SprintDto>> GetAllByProjectAsync(Guid projetId);

        Task<SprintDto> CreateAsync(Guid projetId , CreateSprintDto dto);

        Task<SprintDto> UpdateAsync(Guid sprintId , UpdateSprintDto dto);

        Task StartAsync(Guid sprintId);

        Task CompleteAsync(Guid sprintId);

        Task DeleteAsync(Guid sprintId);

    }
}
