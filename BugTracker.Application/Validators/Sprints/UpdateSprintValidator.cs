using BugTracker.Application.DTOs.Sprints;
using FluentValidation;

namespace BugTracker.Application.Validators.Sprints;

public class UpdateSprintValidator : AbstractValidator<UpdateSprintDto>
{
    public UpdateSprintValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Le nom du sprint est obligatoire.")
            .MaximumLength(100)
            .WithMessage("Le nom du sprint ne peut pas dépasser 100 caractères.");

        RuleFor(x => x.Goal)
            .MaximumLength(500)
            .WithMessage("L'objectif ne peut pas dépasser 500 caractères.")
            .When(x => x.Goal != null);

        RuleFor(x => x)
            .Must(x =>
                !x.StartDate.HasValue ||
                !x.EndDate.HasValue ||
                x.EndDate > x.StartDate)
            .WithMessage(
                "La date de fin doit être strictement postérieure à la date de début.");
    }
}