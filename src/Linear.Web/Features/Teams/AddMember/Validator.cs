using FastEndpoints;

using FluentValidation;

namespace Linear.Web.Features.Teams.AddMember;

public sealed class AddTeamMemberValidator : Validator<AddTeamMemberRequest>
{
    public AddTeamMemberValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("El email es obligatorio.");

        RuleFor(request => request.Role)
            .IsInEnum().WithMessage("El rol indicado no existe.");
    }
}
