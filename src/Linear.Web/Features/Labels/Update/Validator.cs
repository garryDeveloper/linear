using FastEndpoints;

using FluentValidation;

using Linear.Domain.Labels;

namespace Linear.Web.Features.Labels.Update;

public sealed class UpdateLabelValidator : Validator<UpdateLabelRequest>
{
    public UpdateLabelValidator()
    {
        RuleFor(request => request.LabelId)
            .NotEmpty().WithMessage("Falta indicar la label.");

        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre de la label es obligatorio.")
            .MaximumLength(Label.MaxNameLength);

        RuleFor(request => request.Description)
            .MaximumLength(Label.MaxDescriptionLength);
    }
}
