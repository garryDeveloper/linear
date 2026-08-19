using FastEndpoints;

using FluentValidation;

using Linear.Domain.Labels;

namespace Linear.Web.Features.Labels.Create;

public sealed class CreateLabelValidator : Validator<CreateLabelRequest>
{
    public CreateLabelValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre de la label es obligatorio.")
            .MaximumLength(Label.MaxNameLength);

        RuleFor(request => request.Description)
            .MaximumLength(Label.MaxDescriptionLength);
    }
}
