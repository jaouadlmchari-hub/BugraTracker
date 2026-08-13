using BugTracker.Application.DTOs.Attachments;
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
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IFileValidationService fileValidationService,
            IFileStorageService fileStorageService,
            ILogger<AttachmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _fileValidationService = fileValidationService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }
        public async Task<AttachmentDto> UploadAsync(Guid issueId, CreateAttachmentDto dto)
        {
            // 1. Vérifier que l'Issue existe
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new KeyNotFoundException(
                    "Issue non trouvée.");

            // 2. Utilisateur connecté
            var currentUserId = _currentUserService.UserId;

            // 3. Vérifier que l'utilisateur est membre du projet
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUserId);

                if (currentMember == null)
                {
                    throw new UnauthorizedAccessException(
                        "L'utilisateur n'est pas membre du projet.");
                }
            }

            // 4. Vérifier que le fichier existe
            if (dto.FileContent == null)
                throw new InvalidDataException(
                    "Aucun fichier fourni.");

            // 5. Vérifier le nombre maximum de fichiers
            var attachmentCount = await _unitOfWork.Attachments.CountByIssueIdAsync(issueId);

            if (attachmentCount >= 20)
            {
                throw new InvalidDataException(
                    "MAX_ATTACHMENTS_REACHED");
            }

            // 6. Valider le fichier
            await _fileValidationService.ValidateAsync(
                dto.FileContent,
                dto.FileName,
                dto.ContentType);

            // Revenir au début du stream après validation
            if (dto.FileContent.CanSeek)
                dto.FileContent.Position = 0;

            // 7. Générer UUID + extension originale
            var extension = Path.GetExtension(dto.FileName);

            var storageKey =
                $"{Guid.NewGuid()}{extension}";

            // 8. Stocker dans Object Storage
            await _fileStorageService.UploadAsync(
                dto.FileContent,
                storageKey,
                dto.ContentType);

            // 9. Créer l'Attachment
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

            // 10. ActivityLog
            var activityLog = new ActivityLog
            {
                IssueId = issueId,
                UserId = currentUserId,
                Action = ActivityAction.AttachmentAdded
            };

            await _unitOfWork.ActivityLogs
                .AddAsync(activityLog);

            // 11. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            // 12. URL pré-signée valable 1 heure
            var downloadUrl =
                await _fileStorageService
                    .GenerateDownloadUrlAsync(
                        storageKey,
                        TimeSpan.FromHours(1));

            // 13. Retourner le DTO
            return attachment.ToDto(downloadUrl);
        }

        public async Task<string> GetDownloadUrlAsync(Guid attachmentId)
        {
            // 1. Récupérer l'attachment
            var attachment = await _unitOfWork.Attachments
                  .GetByIdWithDetailsAsync(attachmentId);

            if (attachment == null)
                throw new KeyNotFoundException(
                    "Pièce jointe non trouvée.");

            // 2. Récupérer l'utilisateur connecté
            var currentUserId = _currentUserService.UserId;

            // 3. Vérifier que l'utilisateur est membre du projet
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        attachment.Issue.ProjectId,
                        currentUserId);

                if (currentMember == null)
                {
                    throw new UnauthorizedAccessException(
                        "L'utilisateur n'est pas membre du projet.");
                }
            }

            // 4. Générer une URL temporaire
            var downloadUrl = await _fileStorageService
                .GenerateDownloadUrlAsync(
                    attachment.StorageKey,
                    TimeSpan.FromHours(1));

            return downloadUrl;
        }

        public async Task DeleteAsync(Guid attachmentId)
        {
            // 1. Récupérer l'Attachment avec son Issue
            var attachment = await _unitOfWork.Attachments
                .GetByIdWithDetailsAsync(attachmentId);

            if (attachment == null)
                throw new KeyNotFoundException("Pièce jointe non trouvée.");

            var currentUserId = _currentUserService.UserId;

            // 2. Vérifier les permissions
            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        attachment.Issue.ProjectId,
                        currentUserId);

                if (currentMember == null || currentMember.Role != ProjectRole.Manager)
                {
                    throw new UnauthorizedAccessException(
                        "Seuls le PM et l'Admin peuvent supprimer une pièce jointe.");
                }
            }

            // On sauvegarde la clé de stockage avant suppression de l'entité
            var storageKey = attachment.StorageKey;

            // 3. Supprimer le record de la DB et ajouter le Log
            _unitOfWork.Attachments.Delete(attachment);

            var activityLog = new ActivityLog
            {
                IssueId = attachment.IssueId,
                UserId = currentUserId,
                Action = ActivityAction.AttachmentRemoved,
                Field = "Attachment",
                FromValue = storageKey,
                ToValue = null
            };

            await _unitOfWork.ActivityLogs.AddAsync(activityLog);

            // 4. Valider la transaction SQL Server D'ABORD
            await _unitOfWork.SaveChangesAsync();

            // 5. Supprimer le fichier sur S3/MinIO APRES la réussite de la DB
            try
            {
                await _fileStorageService.DeleteAsync(storageKey);
            }
            catch (Exception ex)
            {
                // La DB est déjà à jour. En cas d'échec S3, on log l'erreur 
                // pour un nettoyage ultérieur (ex: via un Background Job / Hangfire)
                _logger.LogError(
                    ex,
                    "Le fichier {StorageKey} n'a pas pu être supprimé de S3 après suppression de la pièce jointe {AttachmentId} en BDD.",
                    storageKey,
                    attachmentId);

                // Optionnel : ne pas relancer d'exception vers l'utilisateur 
                // car la pièce jointe est bel et bien supprimée de l'application.
            }
        }
    }
}
