using FastEndpoints;

using FluentValidation;

namespace Linear.Web.Features.Issues.ChangeStatus;

public sealed class ChangeIssueStatusValidator : Validator<ChangeIssueStatusRequest>
{
    public ChangeIssueStatusValidator() =>
        RuleFor(request => request.Status)
            .IsInEnum().WithMessage("El estado indicado no existe.");
}
