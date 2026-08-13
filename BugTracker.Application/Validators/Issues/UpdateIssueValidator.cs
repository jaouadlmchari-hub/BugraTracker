using BugTracker.Application.DTOs.Issues;
using FluentValidation;

namespace BugTracker.Application.Validators.Issues;

public class UpdateIssueValidator : AbstractValidator<UpdateIssueDto>
{
    public UpdateIssueValidator()
    {
       
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Le titre du ticket est obligatoire.")
            .MinimumLength(5)
            .WithMessage("Le titre doit contenir au moins 5 caractères.")
            .MaximumLength(200)
            .WithMessage("Le titre ne peut pas dépasser 200 caractères.");

       
        RuleFor(x => x.StoryPoints)
            .GreaterThan(0)
            .When(x => x.StoryPoints.HasValue)
            .WithMessage("Les story points doivent être strictement positifs.");

       
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Le type du ticket est invalide.");

       
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("La priorité du ticket est invalide.");

        
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.DueDate.HasValue)
            .WithMessage("La date d'échéance ne peut pas être dans le passé.");
    }
}