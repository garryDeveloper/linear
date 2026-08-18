using FastEndpoints;

using FluentValidation;

using Linear.Domain.Teams;

namespace Linear.Web.Features.Teams.Create;

public sealed class CreateTeamValidator : Validator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre del equipo es obligatorio.")
            .MaximumLength(Team.MaxNameLength);

        RuleFor(request => request.Key)
            .NotEmpty().WithMessage("La clave del equipo es obligatoria.")
            .Length(TeamKey.MinLength, TeamKey.MaxLength);

        RuleFor(request => request.Description)
            .MaximumLength(Team.MaxDescriptionLength);
    }
}
