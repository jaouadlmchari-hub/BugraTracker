using BugTracker.Application.DTOs.Attachments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileValidationService _fileValidationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IFileValidationService fileValidationService,
            IFileStorageService fileStorageService,
            ILogger<AttachmentService> logger,
            IActivityLogService activityLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _fileValidationService = fileValidationService;
            _fileStorageService = fileStorageService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        public async Task<AttachmentDto> UploadAsync(Guid issueId, CreateAttachmentDto dto)
        {
            // 1. Vérifier que l'Issue existe
            var issue = await _unitOfWork.Issues.GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            // 2. Utilisateur connecté
            var currentUserId = _currentUserService.UserId;

            // 3. Vérifier que l'utilisateur est membre du projet
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(issue.ProjectId, currentUserId);

                if (currentMember == null)
                {
                    throw new ForbiddenException("L'utilisateur n'est pas membre du projet.");
                }
            }

            // 4. Vérifier que le fichier existe
            if (dto.FileContent == null)
                throw new BusinessRuleException("Aucun fichier fourni.");

            // 5. Vérifier le nombre maximum de fichiers
            var attachmentCount = await _unitOfWork.Attachments.CountByIssueIdAsync(issueId);

            if (attachmentCount >= 20)
            {
                throw new BusinessRuleException("MAX_ATTACHMENTS_REACHED");
            }

            // 6. Valider le fichier (type, taille, signature/magic bytes)
            await _fileValidationService.ValidateAsync(
                dto.FileContent,
                dto.FileName,
                dto.ContentType);

            if (dto.FileContent.CanSeek)
                dto.FileContent.Position = 0;

            // 7. Générer UUID + extension originale
            var extension = Path.GetExtension(dto.FileName);
            var storageKey = $"{Guid.NewGuid()}{extension}";

            // 8. Stocker dans Object Storage (MinIO / S3)
            await _fileStorageService.UploadAsync(
                dto.FileContent,
                storageKey,
                dto.ContentType);

            // 9. Créer l'Attachment en BDD
            var attachment = new Attachment
            {
                IssueId = issueId,
                UploaderId = currentUserId,
                Filename = dto.FileName,
                StorageKey = storageKey,
                MimeType = dto.ContentType,
                SizeBytes = dto.FileContent.Length
            };

            await _unitOfWork.Attachments.AddAsync(attachment);

            // 10. ActivityLog
            await _activityLogService.LogAsync(issueId, currentUserId, ActivityAction.AttachmentAdded);

            // 11. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 12. URL pré-signée valable 1 heure
            var downloadUrl = await _fileStorageService.GenerateDownloadUrlAsync(
                storageKey,
                TimeSpan.FromHours(1));

            // 13. Retourner le DTO
            return attachment.ToDto(downloadUrl);
        }

        public async Task<string> GetDownloadUrlAsync(Guid attachmentId)
        {
            // 1. Récupérer l'attachment
            var attachment = await _unitOfWork.Attachments.GetByIdWithDetailsAsync(attachmentId);

            if (attachment == null)
                throw new NotFoundException("Pièce jointe non trouvée.");

            // 2. Récupérer l'utilisateur connecté
            var currentUserId = _currentUserService.UserId;

            // 3. Vérifier que l'utilisateur est membre du projet
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(attachment.Issue.ProjectId, currentUserId);

                if (currentMember == null)
                {
                    throw new ForbiddenException("L'utilisateur n'est pas membre du projet.");
                }
            }

            // 4. Générer une URL temporaire
            return await _fileStorageService.GenerateDownloadUrlAsync(
                attachment.StorageKey,
                TimeSpan.FromHours(1));
        }

        public async Task DeleteAsync(Guid attachmentId)
        {
            // 1. Récupérer l'Attachment avec son Issue
            var attachment = await _unitOfWork.Attachments.GetByIdWithDetailsAsync(attachmentId);

            if (attachment == null)
                throw new NotFoundException("Pièce jointe non trouvée.");

            var currentUserId = _currentUserService.UserId;

            // 2. Vérifier les permissions (Manager du projet ou Admin)
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(attachment.Issue.ProjectId, currentUserId);

                if (currentMember == null || currentMember.Role != ProjectRole.Manager)
                {
                    throw new ForbiddenException("Seuls le PM et l'Admin peuvent supprimer une pièce jointe.");
                }
            }

            var storageKey = attachment.StorageKey;

            _unitOfWork.Attachments.Delete(attachment);

            await _activityLogService.LogAsync(
                attachment.IssueId,
                currentUserId,
                ActivityAction.AttachmentRemoved,
                "Attachment",
                storageKey,
                null);

            // 3. Valider la transaction BDD d'abord
            await _unitOfWork.SaveChangesAsync();

            // 4. Supprimer du stockage S3/MinIO après succès BDD
            try
            {
                await _fileStorageService.DeleteAsync(storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Le fichier {StorageKey} n'a pas pu être supprimé de S3 après suppression de la pièce jointe {AttachmentId} en BDD.",
                    storageKey,
                    attachmentId);
            }
        }
    }
}