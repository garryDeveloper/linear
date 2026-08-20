using FastEndpoints;

using FluentValidation;

using Linear.Domain.Roadmaps;

namespace Linear.Web.Features.Roadmaps.CreateItem;

public sealed class CreateRoadmapItemValidator : Validator<CreateRoadmapItemRequest>
{
    public CreateRoadmapItemValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre de la iniciativa es obligatorio.")
            .MaximumLength(RoadmapItem.MaxNameLength);

        RuleFor(request => request.Description)
            .MaximumLength(RoadmapItem.MaxDescriptionLength);

        RuleFor(request => request.TargetDate)
            .GreaterThan(request => request.StartDate)
            .WithMessage("La fecha objetivo tiene que ser posterior a la de inicio.");
    }
}
