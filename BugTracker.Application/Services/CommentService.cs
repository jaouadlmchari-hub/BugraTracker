using BugTracker.Application.DTOs.Audit;
using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BugTracker.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IActivityLogService _activityLogService;
        private readonly IAuditService _auditService;
        private readonly ILogger<CommentService> _logger;

        public CommentService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IActivityLogService activityLogService,
            IAuditService auditService,
            ILogger<CommentService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _activityLogService = activityLogService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CommentDto?> GetByIdAsync(Guid commentId)
        {
            _logger.LogDebug(
                "Getting comment by Id. CommentId: {CommentId}",
                commentId);

            var comment = await _unitOfWork.Comments
                .GetByIdWithDetailsAsync(commentId);

            if (comment == null)
            {
                _logger.LogWarning(
                    "Comment not found. CommentId: {CommentId}",
                    commentId);

                return null;
            }

            return comment.ToDto();
        }

        public async Task<IEnumerable<CommentDto>> GetByIssueAsync(Guid issueId)
        {
            _logger.LogDebug(
                "Getting comments for issue. IssueId: {IssueId}",
                issueId);

            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot get comments because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            var comments = await _unitOfWork.Comments
                .GetByIssueIdAsync(issueId);

            return comments
                .Select(c => c.ToDto())
                .ToList();
        }

        public async Task<CommentDto> CreateAsync(Guid issueId, CreateCommentDto dto)
        {
            var currentUserId =
                _currentUserService.UserId;

            _logger.LogInformation(
                "Creating comment. IssueId: {IssueId}, UserId: {UserId}",
                issueId,
                currentUserId);

            // 1. Vérifier que l'Issue existe
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
            {
                _logger.LogWarning(
                    "Cannot create comment because issue was not found. IssueId: {IssueId}",
                    issueId);

                throw new NotFoundException(
                    "Issue non trouvée.");
            }

            // 2. Vérifier le contenu
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                _logger.LogWarning(
                    "Attempt to create an empty comment. IssueId: {IssueId}, UserId: {UserId}",
                    issueId,
                    currentUserId);

                throw new BusinessRuleException(
                    "Le commentaire ne doit pas être vide.");
            }

            // 3. Créer le commentaire
            var comment = new Comment
            {
                IssueId = issueId,
                AuthorId = currentUserId,
                Content = dto.Content.Trim()
            };

            await _unitOfWork.Comments
                .AddAsync(comment);

            // 4. ActivityLog
            await _activityLogService.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.Commented);

            // 5. Audit Log
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: currentUserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.CommentCreated,
                EntityName: nameof(Comment),
                EntityId: comment.Id.ToString(),
                OldValue: null,
                NewValue: comment.Content,
                Details: $"Ajout d'un commentaire sur l'issue '{issueId}'"
            ));

            // 6. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Comment created successfully. CommentId: {CommentId}, IssueId: {IssueId}, UserId: {UserId}",
                comment.Id,
                issueId,
                currentUserId);

            return comment.ToDto();
        }

        public async Task<CommentDto> UpdateAsync(Guid commentId, UpdateCommentDto dto)
        {
            var currentUserId =
                _currentUserService.UserId;

            _logger.LogInformation(
                "Updating comment. CommentId: {CommentId}, UserId: {UserId}",
                commentId,
                currentUserId);

            // 1. Récupérer le commentaire
            var comment = await _unitOfWork.Comments
                .GetByIdAsync(commentId);

            if (comment == null)
            {
                _logger.LogWarning(
                    "Cannot update comment because it was not found. CommentId: {CommentId}",
                    commentId);

                throw new NotFoundException(
                    "Commentaire non trouvé.");
            }

            // 2. Vérifier la fenêtre de modification de 24 heures
            if (DateTime.UtcNow >
                comment.CreatedAt.AddHours(24))
            {
                _logger.LogWarning(
                    "Comment edit window expired. CommentId: {CommentId}, UserId: {UserId}",
                    commentId,
                    currentUserId);

                throw new BusinessRuleException(
                    "COMMENT_EDIT_WINDOW_EXPIRED");
            }

            // 3. Vérifier le contenu
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                _logger.LogWarning(
                    "Attempt to update comment with empty content. CommentId: {CommentId}, UserId: {UserId}",
                    commentId,
                    currentUserId);

                throw new BusinessRuleException(
                    "Le commentaire ne doit pas être vide.");
            }

            var oldContent = comment.Content;

            // 4. Modifier
            comment.Content = dto.Content.Trim();

            // 5. Audit Log
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: currentUserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.CommentUpdated,
                EntityName: nameof(Comment),
                EntityId: comment.Id.ToString(),
                OldValue: oldContent,
                NewValue: comment.Content,
                Details: $"Mise à jour du commentaire (ID: {comment.Id})"
            ));

            // 6. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Comment updated successfully. CommentId: {CommentId}, UserId: {UserId}",
                commentId,
                currentUserId);

            return comment.ToDto();
        }

        public async Task DeleteAsync(Guid commentId)
        {
            var currentUserId =
                _currentUserService.UserId;

            _logger.LogInformation(
                "Deleting comment. CommentId: {CommentId}, UserId: {UserId}",
                commentId,
                currentUserId);

            // 1. Récupérer le commentaire
            var comment = await _unitOfWork.Comments
                .GetByIdAsync(commentId);

            if (comment == null)
            {
                _logger.LogWarning(
                    "Cannot delete comment because it was not found. CommentId: {CommentId}",
                    commentId);

                throw new NotFoundException(
                    "Commentaire non trouvé.");
            }

            // 2. Supprimer
            _unitOfWork.Comments.Delete(comment);

            // 3. ActivityLog
            await _activityLogService.LogAsync(
                comment.IssueId,
                currentUserId,
                ActivityAction.CommentDeleted);

            // 4. Audit Log
            await _auditService.LogAsync(new CreateAuditLogDto(
                UserId: currentUserId,
                UserEmail: _currentUserService.Email ?? string.Empty,
                Action: AuditAction.CommentDeleted,
                EntityName: nameof(Comment),
                EntityId: comment.Id.ToString(),
                OldValue: comment.Content,
                NewValue: null,
                Details: $"Suppression du commentaire (ID: {comment.Id}) lié à l'issue '{comment.IssueId}'"
            ));

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Comment deleted successfully. CommentId: {CommentId}, IssueId: {IssueId}, UserId: {UserId}",
                commentId,
                comment.IssueId,
                currentUserId);
        }
    }
}