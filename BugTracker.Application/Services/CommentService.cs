using BugTracker.Application.DTOs.Comments;
using BugTracker.Application.Interfaces;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using BugTracker.Application.Mappings;

namespace BugTracker.Application.Services
{
    public class CommentService : ICommentService
    {

        private readonly IUnitOfWork _unitOfWork;

        private readonly ICurrentUserService _currentUserService;

        public CommentService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<CommentDto> CreateAsync(Guid issueId,CreateCommentDto dto)
        {
           
            var issue = await _unitOfWork.Issues
                .GetByIdAsync(issueId);

            if (issue == null)
                throw new KeyNotFoundException("Issue non trouvée.");

        
            var currentUser = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        issue.ProjectId,
                        currentUser);

                if (currentMember == null)
                {
                    throw new UnauthorizedAccessException(
                        "L'utilisateur n'appartient pas à ce projet.");
                }
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new InvalidDataException(
                    "Le commentaire ne doit pas être vide.");
            }

          
            var comment = new Comment
            {
                IssueId = issueId,
                AuthorId = currentUser,
                Content = dto.Content.Trim()
            };

            await _unitOfWork.Comments.AddAsync(comment);

           
            var activityLog = new ActivityLog
            {
                IssueId = issueId,
                UserId = currentUser,
                Action = ActivityAction.Commented
            };

            await _unitOfWork.ActivityLogs.AddAsync(activityLog);

            await _unitOfWork.SaveChangesAsync();

           
            return comment.ToDto();
        }

        public async Task<CommentDto> UpdateAsync(Guid commentId,UpdateCommentDto dto)
        {
            
            var comment = await _unitOfWork.Comments
                .GetByIdAsync(commentId);

            if (comment == null)
                throw new KeyNotFoundException(
                    "Commentaire non trouvé.");

        
            var currentUserId = _currentUserService.UserId;

            if (comment.AuthorId != currentUserId)
            {
                throw new UnauthorizedAccessException(
                    "Seul l'auteur peut modifier ce commentaire.");
            }

            
            if (DateTime.UtcNow > comment.CreatedAt.AddHours(24))
            {
                throw new UnauthorizedAccessException(
                    "COMMENT_EDIT_WINDOW_EXPIRED");
            }

         
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new InvalidDataException(
                    "Le commentaire ne doit pas être vide.");
            }

            
            comment.Content = dto.Content.Trim();

            await _unitOfWork.SaveChangesAsync();

            return comment.ToDto();
        }

        public async Task DeleteAsync(Guid commentId)
        {
            var comment = await _unitOfWork.Comments
                .GetByIdWithDetailsAsync(commentId);

            if (comment == null)
                throw new KeyNotFoundException(
                    "Commentaire non trouvé.");

            var currentUserId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin)
            {
                var currentMember = await _unitOfWork.ProjectMembers
                    .GetByProjectAndUserAsync(
                        comment.Issue.ProjectId,
                        currentUserId);

                var isAuthor = comment.AuthorId == currentUserId;
                var isManager = currentMember?.Role == ProjectRole.Manager;

                if (!isAuthor && !isManager)
                {
                    throw new UnauthorizedAccessException(
                        "Vous n'êtes pas autorisé à supprimer ce commentaire.");
                }
            }

            _unitOfWork.Comments.Delete(comment);

            var activityLog = new ActivityLog
            {
                IssueId = comment.IssueId,
                UserId = currentUserId,
                Action = ActivityAction.CommentDeleted
            };

            await _unitOfWork.ActivityLogs.AddAsync(activityLog);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
