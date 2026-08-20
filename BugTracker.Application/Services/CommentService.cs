using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces;
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

        public async Task<CommentDto> CreateAsync(Guid issueId, CreateCommentDto dto)
        {
            var issue = await _unitOfWork.Issues.GetByIdAsync(issueId);

            if (issue == null)
                throw new NotFoundException("Issue non trouvée.");

            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(issue.ProjectId, currentUserId);

                if (currentMember == null)
                {
                    throw new ForbiddenException("L'utilisateur n'appartient pas à ce projet.");
                }
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new BusinessRuleException("Le commentaire ne doit pas être vide.");
            }

            var comment = new Comment
            {
                IssueId = issueId,
                AuthorId = currentUserId,
                Content = dto.Content.Trim()
            };

            await _unitOfWork.Comments.AddAsync(comment);
            await _activityLogService.LogAsync(issueId, currentUserId, ActivityAction.Commented);
            await _unitOfWork.SaveChangesAsync();

            return comment.ToDto();
        }

        public async Task<CommentDto> UpdateAsync(Guid commentId, UpdateCommentDto dto)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);

            if (comment == null)
                throw new NotFoundException("Commentaire non trouvé.");

            var currentUserId = _currentUserService.UserId;

            if (comment.AuthorId != currentUserId)
            {
                throw new ForbiddenException("Seul l'auteur peut modifier ce commentaire.");
            }

            if (DateTime.UtcNow > comment.CreatedAt.AddHours(24))
            {
                throw new BusinessRuleException("COMMENT_EDIT_WINDOW_EXPIRED");
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new BusinessRuleException("Le commentaire ne doit pas être vide.");
            }

            comment.Content = dto.Content.Trim();

            await _unitOfWork.SaveChangesAsync();

            return comment.ToDto();
        }

        public async Task DeleteAsync(Guid commentId)
        {
            var comment = await _unitOfWork.Comments.GetByIdWithDetailsAsync(commentId);

            if (comment == null)
                throw new NotFoundException("Commentaire non trouvé.");

            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(comment.Issue.ProjectId, currentUserId);

                var isAuthor = comment.AuthorId == currentUserId;
                var isManager = currentMember?.Role == ProjectRole.Manager;

                if (!isAuthor && !isManager)
                {
                    throw new ForbiddenException("Vous n'êtes pas autorisé à supprimer ce commentaire.");
                }
            }

            _unitOfWork.Comments.Delete(comment);
            await _activityLogService.LogAsync(comment.IssueId, currentUserId, ActivityAction.CommentDeleted);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}