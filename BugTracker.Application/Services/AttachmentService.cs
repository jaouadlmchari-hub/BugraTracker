using BugTracker.Application.DTOs.Attachments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
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

        private async Task<string> GenerateDownloadUrlAsync(
            Attachment attachment)
        {
            _logger.LogDebug(
                "Generating download URL. AttachmentId: {AttachmentId}, StorageKey: {StorageKey}",
                attachment.Id,
                attachment.StorageKey);

            return await _fileStorageService.GenerateDownloadUrlAsync(
                attachment.StorageKey,
                TimeSpan.FromHours(1));
        }

        public async Task<AttachmentDto?> GetByIdAsync(
            Guid attachmentId)
        {
            _logger.LogDebug(
                "Getting attachment by Id. AttachmentId: {AttachmentId}",
                attachmentId);

            var attachment = await _unitOfWork.Attachments
                .GetByIdWithDetailsAsync(attachmentId);

            if (attachment == null)
            {
                _logger.LogWarning(
                    "Attachment not found. AttachmentId: {AttachmentId}",
                    attachmentId);

                return null;
            }

            var downloadUrl =
                await GenerateDownloadUrlAsync(attachment);

            return attachment.ToDto(downloadUrl);
        }

        public async Task<IEnumerable<AttachmentDto>> GetByIssueAsync(
            Guid issueId)
        {
            _logger.LogDebug(
                "Getting attachments for issue. IssueId: {IssueId}",
                issueId);

            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot get attachments because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            var attachments = await _unitOfWork.Attachments
                .GetByIssueIdAsync(issueId);

            var result = new List<AttachmentDto>();

            foreach (var attachment in attachments)
            {
                var downloadUrl =
                    await GenerateDownloadUrlAsync(attachment);

                result.Add(
                    attachment.ToDto(downloadUrl));
            }

            _logger.LogDebug(
                "Attachments retrieved successfully. IssueId: {IssueId}, Count: {Count}",
                issueId,
                result.Count);

            return result;
        }

        public async Task<AttachmentDto> UploadAsync(
            Guid issueId,
            CreateAttachmentDto dto)
        {
            var currentUserId =
                _currentUserService.UserId;

            _logger.LogInformation(
                "Uploading attachment. IssueId: {IssueId}, UserId: {UserId}, FileName: {FileName}",
                issueId,
                currentUserId,
                dto.FileName);

            // 1. Vérifier que l'Issue existe
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot upload attachment because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            // 2. Vérifier qu'un fichier a été fourni
            if (dto.FileContent == null)
            {
                _logger.LogWarning(
                    "Upload attempted without file content. IssueId: {IssueId}, UserId: {UserId}",
                    issueId,
                    currentUserId);

                throw new BusinessRuleException(
                    "Aucun fichier fourni.");
            }

            // 3. Vérifier le nombre maximum de fichiers
            var attachmentCount =
                await _unitOfWork.Attachments
                    .CountByIssueIdAsync(issueId);

            if (attachmentCount >= 20)
            {
                _logger.LogWarning(
                    "Maximum attachments reached. IssueId: {IssueId}, CurrentCount: {AttachmentCount}, UserId: {UserId}",
                    issueId,
                    attachmentCount,
                    currentUserId);

                throw new BusinessRuleException(
                    "MAX_ATTACHMENTS_REACHED");
            }

            // 4. Valider le fichier
            _logger.LogDebug(
                "Validating attachment. IssueId: {IssueId}, FileName: {FileName}, ContentType: {ContentType}",
                issueId,
                dto.FileName,
                dto.ContentType);

            try
            {
                await _fileValidationService.ValidateAsync(
                    dto.FileContent,
                    dto.FileName,
                    dto.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Attachment validation failed. IssueId: {IssueId}, FileName: {FileName}, UserId: {UserId}",
                    issueId,
                    dto.FileName,
                    currentUserId);

                throw;
            }

            // Revenir au début du Stream après validation
            if (dto.FileContent.CanSeek)
                dto.FileContent.Position = 0;

            // 5. Générer une clé de stockage unique
            var extension =
                Path.GetExtension(dto.FileName);

            var storageKey =
                $"{Guid.NewGuid()}{extension}";

            // 6. Envoyer le vrai fichier vers MinIO / S3
            _logger.LogDebug(
                "Uploading file to storage. IssueId: {IssueId}, StorageKey: {StorageKey}",
                issueId,
                storageKey);

            try
            {
                await _fileStorageService.UploadAsync(
                    dto.FileContent,
                    storageKey,
                    dto.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "File storage upload failed. IssueId: {IssueId}, StorageKey: {StorageKey}, UserId: {UserId}",
                    issueId,
                    storageKey,
                    currentUserId);

                throw;
            }

            // 7. Enregistrer uniquement les métadonnées en BDD
            var attachment = new Attachment
            {
                IssueId = issueId,
                UploaderId = currentUserId,
                Filename = dto.FileName,
                StorageKey = storageKey,
                MimeType = dto.ContentType,
                SizeBytes = dto.FileContent.Length
            };

            await _unitOfWork.Attachments
                .AddAsync(attachment);

            // 8. ActivityLog
            await _activityLogService.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.AttachmentAdded);

            // 9. Sauvegarder en BDD
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Database save failed after file upload. IssueId: {IssueId}, StorageKey: {StorageKey}",
                    issueId,
                    storageKey);

                // Tentative de nettoyage du fichier déjà uploadé
                try
                {
                    await _fileStorageService.DeleteAsync(
                        storageKey);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(
                        cleanupEx,
                        "Failed to cleanup uploaded file after database failure. StorageKey: {StorageKey}",
                        storageKey);
                }

                throw;
            }

            // 10. Générer une URL pré-signée valable 1 heure
            var downloadUrl =
                await GenerateDownloadUrlAsync(attachment);

            _logger.LogInformation(
                "Attachment uploaded successfully. AttachmentId: {AttachmentId}, IssueId: {IssueId}, UserId: {UserId}, FileName: {FileName}, SizeBytes: {SizeBytes}",
                attachment.Id,
                issueId,
                currentUserId,
                dto.FileName,
                dto.FileContent.Length);

            // 11. Retourner le DTO
            return attachment.ToDto(downloadUrl);
        }

        public async Task<string> GetDownloadUrlAsync(
            Guid attachmentId)
        {
            _logger.LogDebug(
                "Generating download URL for attachment. AttachmentId: {AttachmentId}",
                attachmentId);

            var attachment = await _unitOfWork.Attachments
                .GetByIdAsync(attachmentId);

            if (attachment == null)
            {
                _logger.LogWarning(
                    "Cannot generate download URL because attachment was not found. AttachmentId: {AttachmentId}",
                    attachmentId);

                throw new NotFoundException(
                    "Pièce jointe non trouvée.");
            }

            return await GenerateDownloadUrlAsync(
                attachment);
        }

        public async Task DeleteAsync(
            Guid attachmentId)
        {
            var currentUserId =
                _currentUserService.UserId;

            _logger.LogInformation(
                "Deleting attachment. AttachmentId: {AttachmentId}, UserId: {UserId}",
                attachmentId,
                currentUserId);

            // 1. Récupérer l'Attachment
            var attachment = await _unitOfWork.Attachments
                .GetByIdAsync(attachmentId);

            if (attachment == null)
            {
                _logger.LogWarning(
                    "Cannot delete attachment because it was not found. AttachmentId: {AttachmentId}",
                    attachmentId);

                throw new NotFoundException(
                    "Pièce jointe non trouvée.");
            }

            var storageKey =
                attachment.StorageKey;

            var issueId =
                attachment.IssueId;

            // 2. Supprimer les métadonnées de la BDD
            _unitOfWork.Attachments.Delete(
                attachment);

            // 3. ActivityLog
            await _activityLogService.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.AttachmentRemoved,
                "Attachment",
                storageKey,
                null);

            // 4. Valider d'abord la suppression BDD
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Attachment metadata deleted from database. AttachmentId: {AttachmentId}, IssueId: {IssueId}",
                attachmentId,
                issueId);

            // 5. Puis supprimer le vrai fichier de MinIO / S3
            try
            {
                await _fileStorageService.DeleteAsync(
                    storageKey);

                _logger.LogInformation(
                    "Attachment file deleted from storage successfully. AttachmentId: {AttachmentId}, StorageKey: {StorageKey}",
                    attachmentId,
                    storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "File {StorageKey} could not be deleted from storage after attachment {AttachmentId} was deleted from database.",
                    storageKey,
                    attachmentId);
            }
        }
    }
}