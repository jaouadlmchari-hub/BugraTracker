using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Mappings;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;

namespace BugTracker.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IActivityLogService _activityLogService;

        public CommentService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IActivityLogService activityLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _activityLogService = activityLogService;
        }

        public async Task<CommentDto?> GetByIdAsync(Guid commentId)
        {
            var comment = await _unitOfWork.Comments
                .GetByIdWithDetailsAsync(commentId);

            if (comment == null)
                return null;

            return comment.ToDto();
        }

        public async Task<IEnumerable<CommentDto>> GetByIssueAsync(Guid issueId)
        {
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException(
                    "Issue non trouvée.");

            var comments = await _unitOfWork.Comments
                .GetByIssueIdAsync(issueId);

            return comments
                .Select(c => c.ToDto())
                .ToList();
        }

        public async Task<CommentDto> CreateAsync(Guid issueId, CreateCommentDto dto)
        {
            // 1. Vérifier que l'Issue existe
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException(
                    "Issue non trouvée.");


            // 2. Vérifier le contenu
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new BusinessRuleException(
                    "Le commentaire ne doit pas être vide.");
            }

            // 3. Utilisateur courant nécessaire
            // pour AuthorId et ActivityLog
            var currentUserId =
                _currentUserService.UserId;

            // 4. Créer le commentaire
            var comment = new Comment
            {
                IssueId = issueId,
                AuthorId = currentUserId,
                Content = dto.Content.Trim()
            };

            await _unitOfWork.Comments
                .AddAsync(comment);

            // 5. ActivityLog
            await _activityLogService.LogAsync(
                issueId,
                currentUserId,
                ActivityAction.Commented);

            // 6. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            return comment.ToDto();
        }

        public async Task<CommentDto> UpdateAsync(Guid commentId,UpdateCommentDto dto)
        {
            // 1. Récupérer le commentaire
            var comment = await _unitOfWork.Comments
                .GetByIdAsync(commentId);

            if (comment == null)
                throw new NotFoundException(
                    "Commentaire non trouvé.");

          

            // 2. Règle métier :
            // modification autorisée pendant 24 heures
            if (DateTime.UtcNow >
                comment.CreatedAt.AddHours(24))
            {
                throw new BusinessRuleException(
                    "COMMENT_EDIT_WINDOW_EXPIRED");
            }

            // 3. Vérifier le contenu
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new BusinessRuleException(
                    "Le commentaire ne doit pas être vide.");
            }

            // 4. Modifier
            comment.Content = dto.Content.Trim();

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();

            return comment.ToDto();
        }

        public async Task DeleteAsync(Guid commentId)
        {
            // 1. Récupérer le commentaire
            var comment = await _unitOfWork.Comments
                .GetByIdAsync(commentId);

            if (comment == null)
                throw new NotFoundException(
                    "Commentaire non trouvé.");

           

            // 2. Utilisateur courant nécessaire
            // pour ActivityLog
            var currentUserId =
                _currentUserService.UserId;

            // 3. Supprimer
            _unitOfWork.Comments.Delete(comment);

            // 4. ActivityLog
            await _activityLogService.LogAsync(
                comment.IssueId,
                currentUserId,
                ActivityAction.CommentDeleted);

            // 5. Sauvegarder
            await _unitOfWork.SaveChangesAsync();
        }
    }
}