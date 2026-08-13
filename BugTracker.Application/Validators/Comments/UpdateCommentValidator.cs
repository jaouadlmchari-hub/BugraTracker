using BugTracker.Application.DTOs.Comments;
using FluentValidation;


namespace BugTracker.Application.Validators.Comments
{
    public class UpdateCommentValidator : AbstractValidator<UpdateCommentDto>
    {
        public UpdateCommentValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .Must(content => !string.IsNullOrWhiteSpace(content))
                .WithMessage("Le contenu du commentaire est obligatoire.");
        }
    }
}
