using FastEndpoints;

using FluentValidation;

using Linear.Domain.Sprints;

namespace Linear.Web.Features.Sprints.Create;

public sealed class CreateSprintValidator : Validator<CreateSprintRequest>
{
    public CreateSprintValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre del sprint es obligatorio.")
            .MaximumLength(Sprint.MaxNameLength);

        RuleFor(request => request.Goal)
            .MaximumLength(Sprint.MaxGoalLength);

        RuleFor(request => request.EndDate)
            .GreaterThan(request => request.StartDate)
            .WithMessage("La fecha de fin tiene que ser posterior a la de inicio.");
    }
}
