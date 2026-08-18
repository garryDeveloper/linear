using FastEndpoints;

using FluentValidation;

namespace Linear.Web.Features.Teams.ChangeMemberRole;

public sealed class ChangeTeamMemberRoleValidator : Validator<ChangeTeamMemberRoleRequest>
{
    public ChangeTeamMemberRoleValidator()
    {
        RuleFor(request => request.Role)
            .IsInEnum().WithMessage("El rol indicado no existe.");

        RuleFor(request => request.UserId)
            .NotEmpty().WithMessage("Falta indicar el usuario.");
    }
}
