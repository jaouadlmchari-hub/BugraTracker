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

        private async Task<string> GenerateDownloadUrlAsync(Attachment attachment)
        {
            return await _fileStorageService.GenerateDownloadUrlAsync(
                attachment.StorageKey,
                TimeSpan.FromHours(1));
        }

        public async Task<AttachmentDto?> GetByIdAsync(Guid attachmentId)
        {
            var attachment = await _unitOfWork.Attachments
                .GetByIdWithDetailsAsync(attachmentId);

            if (attachment == null)
                return null;

            var downloadUrl =
                await GenerateDownloadUrlAsync(attachment);

            return attachment.ToDto(downloadUrl);
        }

        public async Task<IEnumerable<AttachmentDto>> GetByIssueAsync(Guid issueId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException(
                    "Issue non trouvée.");

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

            return result;
        }

        public async Task<AttachmentDto> UploadAsync(Guid issueId, CreateAttachmentDto dto)
        {
            // 1. Vérifier que l'Issue existe
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException(
                    "Issue non trouvée.");

            // 2. Utilisateur connecté
            // Nécessaire pour UploaderId et ActivityLog
            var currentUserId =
                _currentUserService.UserId;


            // 3. Vérifier qu'un fichier a été fourni
            if (dto.FileContent == null)
                throw new BusinessRuleException(
                    "Aucun fichier fourni.");

            // 4. Vérifier le nombre maximum de fichiers
            var attachmentCount =
                await _unitOfWork.Attachments
                    .CountByIssueIdAsync(issueId);

            if (attachmentCount >= 20)
            {
                throw new BusinessRuleException(
                    "MAX_ATTACHMENTS_REACHED");
            }

            // 5. Valider le fichier :
            // type, taille, extension, magic bytes...
            await _fileValidationService.ValidateAsync(
                dto.FileContent,
                dto.FileName,
                dto.ContentType);

            // Revenir au début du Stream après validation
            if (dto.FileContent.CanSeek)
                dto.FileContent.Position = 0;

            // 6. Générer une clé de stockage unique
            var extension =
                Path.GetExtension(dto.FileName);

            var storageKey =
                $"{Guid.NewGuid()}{extension}";

            // 7. Envoyer le vrai fichier vers MinIO / S3
            await _fileStorageService.UploadAsync(
                dto.FileContent,
                storageKey,
                dto.ContentType);

            // 8. Enregistrer uniquement les métadonnées en BDD
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

            // 9. ActivityLog
            await _activityLogService.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.AttachmentAdded);

            // 10. Sauvegarder en BDD
            await _unitOfWork.SaveChangesAsync();

            // 11. Générer une URL pré-signée valable 1 heure
            var downloadUrl =
                await GenerateDownloadUrlAsync(attachment);

            // 12. Retourner le DTO
            return attachment.ToDto(downloadUrl);
        }

        public async Task<string> GetDownloadUrlAsync(Guid attachmentId)
        {
            var attachment = await _unitOfWork.Attachments
                .GetByIdAsync(attachmentId);

            if (attachment == null)
                throw new NotFoundException(
                    "Pièce jointe non trouvée.");


            return await GenerateDownloadUrlAsync(
                attachment);
        }

        public async Task DeleteAsync(Guid attachmentId)
        {
            // 1. Récupérer l'Attachment
            var attachment = await _unitOfWork.Attachments
                .GetByIdAsync(attachmentId);

            if (attachment == null)
                throw new NotFoundException(
                    "Pièce jointe non trouvée.");


            var currentUserId =
                _currentUserService.UserId;

            var storageKey =
                attachment.StorageKey;

            // 2. Supprimer les métadonnées de la BDD
            _unitOfWork.Attachments.Delete(
                attachment);

            // 3. ActivityLog
            await _activityLogService.LogAsync(
                attachment.IssueId,
                currentUserId,
                ActivityAction.AttachmentRemoved,
                "Attachment",
                storageKey,
                null);

            // 4. Valider d'abord la suppression BDD
            await _unitOfWork.SaveChangesAsync();

            // 5. Puis supprimer le vrai fichier de MinIO / S3
            try
            {
                await _fileStorageService.DeleteAsync(
                    storageKey);
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