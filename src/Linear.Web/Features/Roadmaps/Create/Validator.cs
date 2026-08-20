using FastEndpoints;

using FluentValidation;

using Linear.Domain.Roadmaps;

namespace Linear.Web.Features.Roadmaps.Create;

public sealed class CreateRoadmapValidator : Validator<CreateRoadmapRequest>
{
    public CreateRoadmapValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre del roadmap es obligatorio.")
            .MaximumLength(Roadmap.MaxNameLength);

        RuleFor(request => request.Description)
            .MaximumLength(Roadmap.MaxDescriptionLength);
    }
}
