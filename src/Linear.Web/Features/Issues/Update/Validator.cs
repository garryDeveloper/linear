using FastEndpoints;

using FluentValidation;

using Linear.Domain.Issues;

namespace Linear.Web.Features.Issues.Update;

public sealed class UpdateIssueValidator : Validator<UpdateIssueRequest>
{
    public UpdateIssueValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("El título del issue es obligatorio.")
            .MaximumLength(Issue.MaxTitleLength);
    }
}
