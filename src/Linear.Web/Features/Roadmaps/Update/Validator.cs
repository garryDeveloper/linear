using FastEndpoints;

using FluentValidation;

using Linear.Domain.Roadmaps;

namespace Linear.Web.Features.Roadmaps.Update;

public sealed class UpdateRoadmapValidator : Validator<UpdateRoadmapRequest>
{
    public UpdateRoadmapValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre del roadmap es obligatorio.")
            .MaximumLength(Roadmap.MaxNameLength);

        RuleFor(request => request.Description)
            .MaximumLength(Roadmap.MaxDescriptionLength);
    }
}
