using BugTracker.Application.DTOs.Epics;
using BugTracker.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Application.Interfaces.Services
{
    public interface IEpicService
    {
        Task<EpicDto> CreateAsync(Guid projectId, CreateEpicDto dto);

        Task<EpicDto> UpdateAsync(Guid epicId, UpdateEpicDto dto);

        Task<EpicDto?> GetByIdAsync(Guid epicId);

        Task<EpicDetailsDto?> GetByIdWithDetailsAsync(Guid epicId);

        Task DeleteAsync(Guid epicId);

        Task ChangeStatusAsync(Guid epicId, EpicStatus newStatus);

    }
}
