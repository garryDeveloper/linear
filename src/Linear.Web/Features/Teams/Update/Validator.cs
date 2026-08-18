using FastEndpoints;

using FluentValidation;

using Linear.Domain.Teams;

namespace Linear.Web.Features.Teams.Update;

public sealed class UpdateTeamValidator : Validator<UpdateTeamRequest>
{
    public UpdateTeamValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre del equipo es obligatorio.")
            .MaximumLength(Team.MaxNameLength);

        RuleFor(request => request.Description)
            .MaximumLength(Team.MaxDescriptionLength);
    }
}
