using BugTracker.Application.DTOs.Sprints;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services
{
    public class SprintService : ISprintService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SprintService> _logger;

        public SprintService(
            IUnitOfWork unitOfWork,
            ILogger<SprintService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SprintDto?> GetByIdAsync(Guid sprintId)
        {
            _logger.LogDebug(
                "Retrieving sprint {SprintId}.",
                sprintId);

            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
            {
                _logger.LogWarning(
                    "Sprint {SprintId} was not found.",
                    sprintId);

                return null;
            }

            return sprint.ToDto();
        }

        public async Task<IEnumerable<SprintDto>> GetAllByProjectAsync(
            Guid projectId)
        {
            _logger.LogDebug(
                "Retrieving sprints for project {ProjectId}.",
                projectId);

            var sprints = await _unitOfWork.Sprints
                .GetByProjectIdAsync(projectId);

            _logger.LogDebug(
                "Retrieved {SprintCount} sprints for project {ProjectId}.",
                sprints.Count(),
                projectId);

            return sprints
                .Select(s => s.ToDto())
                .ToList();
        }

        public async Task<SprintDto> CreateAsync(
            Guid projectId,
            CreateSprintDto dto)
        {
            _logger.LogInformation(
                "Creating sprint {SprintName} for project {ProjectId}.",
                dto.Name,
                projectId);

            var project = await _unitOfWork.Projects
                .GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning(
                    "Sprint creation failed. Project {ProjectId} was not found.",
                    projectId);

                throw new NotFoundException(
                    "Projet non trouvé.");
            }

            if (dto.StartDate.HasValue &&
                dto.EndDate.HasValue &&
                dto.EndDate <= dto.StartDate)
            {
                _logger.LogWarning(
                    "Sprint creation rejected for project {ProjectId}. " +
                    "End date must be after start date.",
                    projectId);

                throw new BusinessRuleException(
                    "La date de fin doit être postérieure à la date de début.");
            }

            var sprint = new Sprint
            {
                ProjectId = projectId,
                Name = dto.Name,
                Goal = dto.Goal,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = SprintStatus.Planning
            };

            await _unitOfWork.Sprints.AddAsync(sprint);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Sprint {SprintId} created successfully for project {ProjectId}.",
                sprint.Id,
                projectId);

            return sprint.ToDto();
        }

        public async Task<SprintDto> UpdateAsync(
            Guid sprintId,
            UpdateSprintDto dto)
        {
            _logger.LogInformation(
                "Updating sprint {SprintId}.",
                sprintId);

            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
            {
                _logger.LogWarning(
                    "Sprint update failed. Sprint {SprintId} was not found.",
                    sprintId);

                throw new NotFoundException(
                    "Sprint non trouvé.");
            }

            if (sprint.Status != SprintStatus.Planning)
            {
                _logger.LogWarning(
                    "Sprint update rejected. Sprint {SprintId} is not in Planning status.",
                    sprintId);

                throw new BusinessRuleException(
                    "Seul un sprint en Planning peut être modifié.");
            }

            if (dto.StartDate.HasValue &&
                dto.EndDate.HasValue &&
                dto.EndDate <= dto.StartDate)
            {
                _logger.LogWarning(
                    "Sprint update rejected. Invalid date range for sprint {SprintId}.",
                    sprintId);

                throw new BusinessRuleException(
                    "La date de fin doit être postérieure à la date de début.");
            }

            sprint.Name = dto.Name;
            sprint.Goal = dto.Goal;
            sprint.StartDate = dto.StartDate;
            sprint.EndDate = dto.EndDate;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Sprint {SprintId} updated successfully.",
                sprintId);

            return sprint.ToDto();
        }

        public async Task StartAsync(Guid sprintId)
        {
            _logger.LogInformation(
                "Starting sprint {SprintId}.",
                sprintId);

            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
            {
                _logger.LogWarning(
                    "Start sprint failed. Sprint {SprintId} was not found.",
                    sprintId);

                throw new NotFoundException(
                    "Sprint non trouvé.");
            }

            if (sprint.Status != SprintStatus.Planning)
            {
                _logger.LogWarning(
                    "Start sprint rejected. Sprint {SprintId} is not in Planning status.",
                    sprintId);

                throw new BusinessRuleException(
                    "Seul un sprint en Planning peut être démarré.");
            }

            var activeSprints = await _unitOfWork.Sprints
                .GetActiveSprintsAsync(sprint.ProjectId);

            if (activeSprints.Any())
            {
                _logger.LogWarning(
                    "Start sprint rejected. Project {ProjectId} already has an active sprint.",
                    sprint.ProjectId);

                throw new BusinessRuleException(
                    "Un sprint est déjà actif pour ce projet.");
            }

            sprint.Status = SprintStatus.Active;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Sprint {SprintId} started successfully for project {ProjectId}.",
                sprintId,
                sprint.ProjectId);
        }

        public async Task CompleteAsync(Guid sprintId)
        {
            _logger.LogInformation(
                "Completing sprint {SprintId}.",
                sprintId);

            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
            {
                _logger.LogWarning(
                    "Complete sprint failed. Sprint {SprintId} was not found.",
                    sprintId);

                throw new NotFoundException(
                    "Sprint non trouvé.");
            }

            if (sprint.Status != SprintStatus.Active)
            {
                _logger.LogWarning(
                    "Complete sprint rejected. Sprint {SprintId} is not active.",
                    sprintId);

                throw new BusinessRuleException(
                    "Seul un sprint actif peut être terminé.");
            }

            var unfinishedIssues = await _unitOfWork.Issues
                .GetUnfinishedBySprintIdAsync(sprintId);

            var unfinishedIssueCount = unfinishedIssues.Count();

            foreach (var issue in unfinishedIssues)
            {
                issue.SprintId = null;
            }

            sprint.Status = SprintStatus.Completed;
            sprint.CompletedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Sprint {SprintId} completed successfully. " +
                "{IssueCount} unfinished issues were moved back to the backlog.",
                sprintId,
                unfinishedIssueCount);
        }

        public async Task DeleteAsync(Guid sprintId)
        {
            _logger.LogInformation(
                "Deleting sprint {SprintId}.",
                sprintId);

            var sprint = await _unitOfWork.Sprints
                .GetByIdAsync(sprintId);

            if (sprint == null)
            {
                _logger.LogWarning(
                    "Sprint deletion failed. Sprint {SprintId} was not found.",
                    sprintId);

                throw new NotFoundException(
                    "Sprint non trouvé.");
            }

            if (sprint.Status == SprintStatus.Active)
            {
                _logger.LogWarning(
                    "Sprint deletion rejected. Sprint {SprintId} is active.",
                    sprintId);

                throw new BusinessRuleException(
                    "Un sprint actif ne peut pas être supprimé.");
            }

            _unitOfWork.Sprints.Delete(sprint);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Sprint {SprintId} deleted successfully.",
                sprintId);
        }
    }
}