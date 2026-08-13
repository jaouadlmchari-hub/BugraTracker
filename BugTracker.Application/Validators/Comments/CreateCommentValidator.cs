using BugTracker.Application.DTOs.Comments;
using FluentValidation;


namespace BugTracker.Application.Validators.Comments
{
    public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .Must(content => !string.IsNullOrWhiteSpace(content))
                .WithMessage("Le contenu du commentaire est obligatoire.");
        }
    }
}
