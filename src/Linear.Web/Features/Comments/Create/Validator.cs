using FastEndpoints;

using FluentValidation;

using Linear.Domain.Comments;

namespace Linear.Web.Features.Comments.Create;

public sealed class CreateCommentValidator : Validator<CreateCommentRequest>
{
    public CreateCommentValidator()
    {
        RuleFor(request => request.Content)
            .NotEmpty().WithMessage("El comentario no puede estar vacío.")
            .MaximumLength(Comment.MaxContentLength);
    }
}
