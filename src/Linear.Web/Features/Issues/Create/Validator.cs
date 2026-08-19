using FastEndpoints;

using FluentValidation;

using Linear.Domain.Issues;

namespace Linear.Web.Features.Issues.Create;

public sealed class CreateIssueValidator : Validator<CreateIssueRequest>
{
    public CreateIssueValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("El título del issue es obligatorio.")
            .MaximumLength(Issue.MaxTitleLength);
    }
}
