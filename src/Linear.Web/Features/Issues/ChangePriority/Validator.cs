using FastEndpoints;

using FluentValidation;

namespace Linear.Web.Features.Issues.ChangePriority;

public sealed class ChangeIssuePriorityValidator : Validator<ChangeIssuePriorityRequest>
{
    public ChangeIssuePriorityValidator() =>
        RuleFor(request => request.Priority)
            .IsInEnum().WithMessage("La prioridad indicada no existe.");
}
